using IntelliDoc.Modules.Extraction.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace IntelliDoc.Modules.Extraction.Endpoints;

[Authorize]
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly ExtractionDbContext _dbContext;

    public AnalyticsController(ExtractionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        // 1. Kullanıcıyı Bul (Sadece kendi verisini görsün)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // 2. Veritabanından Kullanıcının Tüm Sonuçlarını Çek
        // Not: Çok büyük veride bu kısmı SQL sorgusu ile yapmak daha performanslıdır.
        // Şimdilik MVP için Memory'e çekip işliyoruz.
        var results = await _dbContext.ExtractionResults
            .Where(x => x.UserId == userId)
            .Select(x => x.JsonData)
            .ToListAsync();

        var stats = new AnalyticsDashboardDto
        {
            TotalDocuments = results.Count,
            TotalSpend = 0,
            MonthlyTrend = new List<MonthlySpend>(),
            TopVendors = new List<VendorSpend>()
        };

        // Ara Bellek Listeleri
        var vendorMap = new Dictionary<string, decimal>();
        var monthMap = new Dictionary<string, decimal>();

        foreach (var json in results)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Güvenli Erişim
                if (root.TryGetProperty("extracted_data", out var data) &&
                    data.TryGetProperty("Entities", out var entities))
                {
                    // Tutar (Amount)
                    decimal amount = 0;
                    if (entities.TryGetProperty("Amount", out var amtEl))
                    {
                        if (amtEl.ValueKind == JsonValueKind.Number) amount = amtEl.GetDecimal();
                        else if (amtEl.ValueKind == JsonValueKind.String) decimal.TryParse(amtEl.GetString(), out amount);
                    }
                    stats.TotalSpend += amount;

                    // Tarih (Date) -> Ay Bazlı
                    if (entities.TryGetProperty("Date", out var dateEl))
                    {
                        var dateStr = dateEl.GetString();
                        if (DateTime.TryParse(dateStr, out var date))
                        {
                            var monthKey = date.ToString("yyyy-MM"); // 2024-01
                            if (!monthMap.ContainsKey(monthKey)) monthMap[monthKey] = 0;
                            monthMap[monthKey] += amount;
                        }
                    }

                    // Tedarikçi (Sender)
                    string vendor = "BİLİNMEYEN"; // Varsayılan büyük harf
                    if (entities.TryGetProperty("Sender", out var senderEl))
                    {
                        // 1. String'i al
                        var rawName = senderEl.GetString();

                        if (!string.IsNullOrWhiteSpace(rawName))
                        {
                            // 2. Temizle ve BÜYÜK HARFE çevir (Türkçe karakter duyarlı)
                            vendor = rawName.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"));
                        }
                    }

                    // 3. Ekle (Artık hepsi D-MARKET... olacak)
                    if (!vendorMap.ContainsKey(vendor)) vendorMap[vendor] = 0;
                    vendorMap[vendor] += amount;
                }
            }
            catch
            {
                // JSON hatalıysa atla
            }
        }

        // 3. Listeleri Doldur ve Sırala

        // Aylık Trend (Eskiden Yeniye)
        stats.MonthlyTrend = monthMap
            .Select(x => new MonthlySpend { Month = x.Key, Amount = x.Value })
            .OrderBy(x => x.Month)
            .ToList();

        // En Çok Harcanan 5 Tedarikçi
        stats.TopVendors = vendorMap
            .Select(x => new VendorSpend { Name = x.Key, Amount = x.Value })
            .OrderByDescending(x => x.Amount)
            .Take(5)
            .ToList();

        return Ok(stats);
    }
}