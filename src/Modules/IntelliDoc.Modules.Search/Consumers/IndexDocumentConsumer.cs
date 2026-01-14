using IntelliDoc.Modules.Search.Services;
using IntelliDoc.Shared.Events;
using MassTransit;
using System.Text.Json;

namespace IntelliDoc.Modules.Search.Consumers;

public class IndexDocumentConsumer : IConsumer<IDataExtracted>
{
    private readonly SearchService _searchService;

    public IndexDocumentConsumer(SearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task Consume(ConsumeContext<IDataExtracted> context)
    {
        var message = context.Message;
        Console.WriteLine($"[Search] İndexleme başladı: {message.DocumentId}");

        await _searchService.CreateIndexIfNotExistsAsync();

        // 1. TEMİZLİK: Gemini bazen ```json etiketiyle gönderiyor, onu temizleyelim.
        // message.RawText, Gemini'den gelen ham cevaptır.
        string cleanJson = message.RawText ?? "";

        if (cleanJson.Contains("```"))
        {
            cleanJson = cleanJson.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                                 .Replace("```", "")
                                 .Trim();
        }

        // Değişkenleri hazırla
        string summary = "", sender = "", date = "", docType = "";
        decimal amount = 0;

        try
        {
            // 2. PARSE: Temizlenmiş veriyi oku
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            // 3. MAPLEME: Alanları tek tek çek
            // (Büyük/Küçük harf duyarlılığı olmaması için güvenli okuma yapıyoruz)

            if (root.TryGetProperty("DocumentType", out var dt)) docType = dt.GetString() ?? "";
            if (root.TryGetProperty("Summary", out var sm)) summary = sm.GetString() ?? "";

            if (root.TryGetProperty("Entities", out var ent))
            {
                if (ent.TryGetProperty("Sender", out var snd)) sender = snd.GetString() ?? "";
                if (ent.TryGetProperty("Date", out var d)) date = d.GetString() ?? "";

                // Tutar sayısal mı string mi kontrol et
                if (ent.TryGetProperty("Amount", out var amt))
                {
                    if (amt.ValueKind == JsonValueKind.Number)
                        amount = amt.GetDecimal();
                    else if (amt.ValueKind == JsonValueKind.String && decimal.TryParse(amt.GetString(), out var parsedAmt))
                        amount = parsedAmt;
                }
            }
        }
        catch (JsonException)
        {
            Console.WriteLine("[Search] JSON Parse Hatası: Veri düzgün formatta değil.");
            // Parse edilemese bile en azından RawText kaydedilsin diye devam ediyoruz.
        }

        // 4. KAYIT: Elasticsearch nesnesini oluştur
        var searchDoc = new SearchDocument
        {
            Id = message.DocumentId,
            DocumentType = docType,
            Content = cleanJson,
            Summary = summary,
            Sender = sender,
            Date = date,
            Amount = amount,
            IndexedAt = DateTime.UtcNow
        };

        await _searchService.IndexDocumentAsync(searchDoc);
        Console.WriteLine($"[Search] ✅ ElasticSearch'e kaydedildi. (Sender: {sender}, Amount: {amount})");
    }
}