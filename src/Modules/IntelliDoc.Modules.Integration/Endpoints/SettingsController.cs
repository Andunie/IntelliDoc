using IntelliDoc.Modules.Integration.Data;
using IntelliDoc.Modules.Integration.Entities;
using IntelliDoc.Modules.Integration.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace IntelliDoc.Modules.Integration.Endpoints;

[Authorize]
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase    
{
    private readonly IntegrationDbContext _dbContext;
    private readonly WebhookSender _webhookSender;

    public SettingsController(IntegrationDbContext dbContext, WebhookSender webhookSender)
    {
        _dbContext = dbContext;
        _webhookSender = webhookSender;
    }

    // Mevcut ayarı getir
    [HttpGet("webhook")]
    public async Task<IActionResult> GetWebhook()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var webhook = await _dbContext.Webhooks.FirstOrDefaultAsync(x => x.UserId == userId);

        // Eğer yoksa boş dön (Frontend ona göre davranır)
        if (webhook == null) return Ok(new { isActive = false, endpointUrl = "" });

        return Ok(webhook);
    }

    // Ayarı Kaydet/Güncelle
    [HttpPost("webhook")]
    public async Task<IActionResult> SaveWebhook([FromBody] SaveWebhookRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var webhook = await _dbContext.Webhooks.FirstOrDefaultAsync(x => x.UserId == userId);

        if (webhook == null)
        {
            webhook = new WebhookSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                // --- EKLENECEK SATIR ---
                Secret = Guid.NewGuid().ToString("N") // Otomatik Secret üret
            };
            _dbContext.Webhooks.Add(webhook);
        }

        webhook.EndpointUrl = request.Url;
        webhook.IsActive = request.IsActive;
        // Secret key opsiyonel, şimdilik boş geçebiliriz veya request'ten alabiliriz.

        await _dbContext.SaveChangesAsync();

        return Ok(webhook);
    }

    // Test Butonu İçin
    [HttpPost("webhook/test")]
    public async Task<IActionResult> TestWebhook()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Veritabanındaki ayarı bul
        var webhook = await _dbContext.Webhooks.FirstOrDefaultAsync(x => x.UserId == userId);

        if (webhook == null || string.IsNullOrEmpty(webhook.EndpointUrl))
            return BadRequest("Önce geçerli bir URL kaydedin.");

        // Örnek bir veri gönderelim
        var testPayload = new
        {
            Event = "TestWebhook",
            Message = "Merhaba! Bu IntelliDoc'tan gelen bir test mesajıdır. 🚀",
            Timestamp = DateTime.UtcNow
        };

        await _webhookSender.SendAsync(webhook.EndpointUrl, testPayload);

        return Ok(new { Message = "Test isteği gönderildi." });
    }
}

// DTO (Data Transfer Object)
public class SaveWebhookRequest
{
    public string Url { get; set; }
    public bool IsActive { get; set; }
}