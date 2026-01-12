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

        // Önce index'in var olduğundan emin ol
        await _searchService.CreateIndexIfNotExistsAsync();

        // JSON verisini parçala
        using var doc = JsonDocument.Parse(message.JsonData);
        var root = doc.RootElement;

        string summary = "", sender = "", date = "";
        decimal amount = 0;

        // extracted_data -> Summary / Entities içinden verileri çek
        if (root.TryGetProperty("extracted_data", out var data))
        {
            if (data.TryGetProperty("Summary", out var s)) summary = s.GetString() ?? "";

            if (data.TryGetProperty("Entities", out var ent))
            {
                if (ent.TryGetProperty("Sender", out var snd)) sender = snd.GetString() ?? "";
                if (ent.TryGetProperty("Date", out var d)) date = d.GetString() ?? "";
                if (ent.TryGetProperty("Amount", out var amt) && amt.ValueKind == JsonValueKind.Number)
                    amount = amt.GetDecimal();
            }
        }

        // Elasticsearch için modeli hazırla
        var searchDoc = new SearchDocument
        {
            Id = message.DocumentId,
            Content = message.RawText, // CV'nin veya Faturanın tüm metni
            Summary = summary,
            Sender = sender,
            Date = date,
            Amount = amount,
            IndexedAt = DateTime.UtcNow
        };

        // Kaydet
        await _searchService.IndexDocumentAsync(searchDoc);
        Console.WriteLine($"[Search] ✅ ElasticSearch'e kaydedildi.");
    }
}