using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Configuration;

namespace IntelliDoc.Modules.Search.Services;

public class SearchService
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "documents";

    public SearchService(IConfiguration config)
    {
        // Docker'daki Elastic adresi (http://localhost:9200)
        var url = config["ElasticSearch:Uri"] ?? "http://localhost:9200";

        var settings = new ElasticsearchClientSettings(new Uri(url))
            .DefaultIndex(IndexName)
            .DisableDirectStreaming(); // Debug için

        _client = new ElasticsearchClient(settings);
    }

    // 1. İndex Oluştur (Eğer yoksa)
    public async Task CreateIndexIfNotExistsAsync()
    {
        var exists = await _client.Indices.ExistsAsync(IndexName);
        if (!exists.Exists)
        {
            await _client.Indices.CreateAsync(IndexName);
        }
    }

    // 2. Veri Ekle (Indexing)
    public async Task IndexDocumentAsync(SearchDocument doc)
    {
        await _client.IndexAsync(doc);
    }

    // 3. Arama Yap (Full Text Search)
    public async Task<List<SearchDocument>> SearchAsync(string query, string userId)
    {
        var response = await _client.SearchAsync<SearchDocument>(s => s
            .Index(IndexName)
            .Query(q => q
                .Bool(b => b
                    // 1. Kısım: Sadece bu UserId (Zorunlu Filtre)
                    .Filter(f => f
                        .Term(t => t.Field(doc => doc.UserId.Suffix("keyword")).Value(userId))
                    )
                    // 2. Kısım: Kelime Araması (Zorunlu Eşleşme)
                    .Must(m => m
                        .MultiMatch(mm => mm
                            .Fields(new[] { "content", "summary", "sender", "documentType" }) // Düz String Array
                            .Query(query)
                            .Fuzziness(new Fuzziness("AUTO"))
                        )
                    )
                )
            )
        );

        if (!response.IsValidResponse)
        {
            // Hata varsa boş dön
            return new List<SearchDocument>();
        }

        return response.Documents.ToList();
    }
}

// Elasticsearch'e atacağımız veri modeli
public class SearchDocument
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string DocumentType { get; set; } 
    public string Content { get; set; }
    public string Summary { get; set; }
    public string Sender { get; set; }
    public string Date { get; set; }
    public decimal Amount { get; set; }
    public DateTime IndexedAt { get; set; }
}