using System.Net.Http.Json;
using System.Text.Json;

namespace IntelliDoc.Modules.Extraction.Services;

public class AiExtractionService
{
    private readonly HttpClient _httpClient;

    public AiExtractionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Python servisi:
        _httpClient.BaseAddress = new Uri("http://localhost:8000");
    }

    public async Task<AiResponse?> ExtractDataAsync(Stream fileStream, string fileName)
    {
        string contentType = "application/octet-stream";
        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            contentType = "application/pdf";
        else if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            contentType = "image/jpeg";
        else if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            contentType = "image/png";

        // Multipart Form Data
        using var content = new MultipartFormDataContent();

        // ÖNEMLİ DEĞİŞİKLİK BURADA:
        var fileContent = new StreamContent(fileStream);
        // Dosyanın Content-Type başlığını ekliyoruz
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        content.Add(fileContent, "file", fileName);

        // Python'a POST isteği at
        var response = await _httpClient.PostAsync("/extract", content);
        response.EnsureSuccessStatusCode();

        // Gelen JSON cevabını oku
        return await response.Content.ReadFromJsonAsync<AiResponse>();
    }
}

// Python'dan dönen cevabın modeli
public class AiResponse
{
    public string? filename { get; set; }
    public string? text { get; set; }
    public double? confidence { get; set; }
    public string? error { get; set; }
}

public class ExtractedData
{
    public string? DocumentType { get; set; } // Fatura, Bordro...
    public string? Summary { get; set; }
    public Entities? Entities { get; set; }
    public List<LineItem>? LineItems { get; set; }
}

public class Entities
{
    public string? Date { get; set; }
    public decimal? Amount { get; set; }
    public string? Sender { get; set; }
    public string? Receiver { get; set; }
    public string? InvoiceNumber { get; set; }
}

public class LineItem
{
    public string? Description { get; set; }
    public string? Value { get; set; } // Tutar veya sayı
}