using IntelliDoc.Modules.Extraction.Data;
using IntelliDoc.Modules.Extraction.Entities;
using IntelliDoc.Modules.Extraction.Services;
using IntelliDoc.Shared.Events;
using MassTransit;
using Minio;
using Minio.DataModel.Args;
using System.Text.Json;

namespace IntelliDoc.Modules.Extraction.Consumers;

public class ExtractDocumentConsumer : IConsumer<IDocumentUploaded>
{
    private readonly IMinioClient _minioClient;
    private readonly AiExtractionService _aiService;
    private readonly ExtractionDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public ExtractDocumentConsumer(IMinioClient minioClient, AiExtractionService aiService, ExtractionDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _minioClient = minioClient;
        _aiService = aiService;
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<IDocumentUploaded> context)
    {
        var message = context.Message;
        Console.WriteLine($"[Extraction] Dosya işleniyor: {message.FileName}");

        // 1. Dosyayı MinIO'dan İndir
        using var memoryStream = new MemoryStream();
        var getObjectArgs = new GetObjectArgs()
            .WithBucket("documents")
            .WithObject(message.FilePath)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream));

        await _minioClient.GetObjectAsync(getObjectArgs);
        memoryStream.Position = 0; // Başa sar

        // 2. Python AI Servisine Gönder
        var aiResult = await _aiService.ExtractDataAsync(memoryStream, message.FileName);

        // HATA VARSA YAKALA
        if (aiResult != null && !string.IsNullOrEmpty(aiResult.error))
        {
            Console.WriteLine($"[Extraction] ❌ AI Servis Hatası: {aiResult.error}");
            // Burada return diyip işlemi durdurabilir veya hatayı veritabanına yazabiliriz.
            // Şimdilik sadece görelim.
        }

        if (aiResult != null)
        {
            // 3. Sonucu Veritabanına Kaydet
            var result = new ExtractionResult
            {
                Id = Guid.NewGuid(),
                DocumentId = message.DocumentId,
                RawText = aiResult.text ?? "",
                JsonData = JsonSerializer.Serialize(aiResult), // Şimdilik tüm cevabı basıyoruz
                ProcessedAt = DateTime.UtcNow,
                ConfidenceScore = aiResult.confidence ?? 0
            };

            _dbContext.ExtractionResults.Add(result);
            await _dbContext.SaveChangesAsync();

            if (!string.IsNullOrEmpty(aiResult.text))
            {
                Console.WriteLine($"[Extraction] Başarılı! Metin uzunluğu: {aiResult.text.Length}");
            }
            else
            {
                Console.WriteLine("[Extraction] İşlem bitti ama metin çıkarılamadı (AI cevabı boş).");
            }

            await _publishEndpoint.Publish<IDataExtracted>(new
            {
                DocumentId = message.DocumentId,
                RawText = aiResult.text ?? "",
                JsonData = JsonSerializer.Serialize(aiResult),
                Success = true,
                ErrorMessage = (string?)null
            });

            Console.WriteLine("[Extraction] -> [Audit] Event fırlatıldı. 🚀");
        }
    }
}