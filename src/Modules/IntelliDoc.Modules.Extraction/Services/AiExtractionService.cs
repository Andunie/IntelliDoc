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
    public ExtractedData? extracted_data { get; set; }
    public double? confidence { get; set; }
    public string? error { get; set; }
}

public class ExtractedData
{
    public string? DocumentType { get; set; } // Fatura, Bordro, CV...
    public string? Summary { get; set; }
    public Dictionary<string, object>? Entities { get; set; }

    // Tablolar (İş Deneyimi, Fatura Kalemleri...)
    public List<ExtractedTable>? Tables { get; set; }

    // Geriye dönük uyumluluk (Eğer Gemini Fields dönerse)
    public Dictionary<string, object>? Fields
    {
        get => Entities;
        set => Entities = value;
    }
}

public class ExtractedTable
{
    public string? Name { get; set; } // Tablonun adı (Örn: "LineItems")

    // Tablo satırları da dinamik sözlüklerdir
    public List<Dictionary<string, object>>? Rows { get; set; }
}