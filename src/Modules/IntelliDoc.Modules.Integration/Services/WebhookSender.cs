using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Integration.Services;

public class WebhookSender
{
    private readonly HttpClient _httpClient;

    public WebhookSender(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendAsync(string url, object payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(url, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Webhook] Hata: {ex.Message}");
        }
    }
}