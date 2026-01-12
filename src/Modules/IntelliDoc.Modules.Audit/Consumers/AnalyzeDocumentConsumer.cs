using IntelliDoc.Modules.Audit.Data;
using IntelliDoc.Modules.Audit.Entities;
using IntelliDoc.Shared.Events;
using MassTransit;
using System.Text.Json;

namespace IntelliDoc.Modules.Audit.Consumers;

public class AnalyzeDocumentConsumer : IConsumer<IDataExtracted>
{
    private readonly AuditDbContext _dbContext;

    public AnalyzeDocumentConsumer(AuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<IDataExtracted> context)
    {
        var message = context.Message;
        Console.WriteLine($"[Audit] Enterprise Audit Başladı: {message.DocumentId}");

        // 1. Audit Record Oluştur (Belge Seviyesi)
        var auditRecord = new AuditRecord
        {
            Id = Guid.NewGuid(),
            DocumentId = message.DocumentId,
            DocumentHash = "SHA256_PLACEHOLDER", // İleride gerçek hash hesaplanır
            ModelName = "gemini-1.5-flash",
            ModelVersion = "v1.0",
            ProcessedAt = DateTime.UtcNow
        };

        // 2. Field Level Tracking (JSON'u parçala ve alanları kaydet)
        // Her kritik alan için bir FieldHistory oluşturuyoruz (İlk değer)
        using var doc = JsonDocument.Parse(message.JsonData);
        var root = doc.RootElement;

        // Örnek: "Entities" altındaki alanları gezelim
        if (root.TryGetProperty("extracted_data", out var data) &&
            data.TryGetProperty("Entities", out var entities))
        {
            foreach (var property in entities.EnumerateObject())
            {
                // Değer null değilse kaydet
                string value = property.Value.ToString();

                auditRecord.FieldHistories.Add(new FieldHistory
                {
                    Id = Guid.NewGuid(),
                    FieldName = property.Name, // "Amount", "Date" vs.
                    OldValue = null, // İlk kayıt olduğu için eskisi yok
                    NewValue = value,
                    ConfidenceScore = 1.0, // Gemini her zaman emindir :)
                    ChangedBy = "SYSTEM (AI)",
                    ChangeReason = "Initial Extraction",
                    ChangedAt = DateTime.UtcNow
                });
            }
        }

        _dbContext.AuditRecords.Add(auditRecord);

        // 3. Business Rules (Eski mantık buraya taşındı)
        // Tutar kontrolü vb.
        // ... (Burayı şimdilik basit tutabiliriz veya ekleyebiliriz)

        await _dbContext.SaveChangesAsync();
        Console.WriteLine($"[Audit] {auditRecord.FieldHistories.Count} alan takip altına alındı.");
    }
}