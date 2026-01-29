using IntelliDoc.Modules.Intake.Data;
using IntelliDoc.Modules.Intake.Entities; // DocumentStatus enum'ı burada
using IntelliDoc.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace IntelliDoc.Modules.Intake.Consumers;

public class DocumentApprovedConsumer : IConsumer<IDocumentApproved>
{
    private readonly IntakeDbContext _dbContext;

    public DocumentApprovedConsumer(IntakeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<IDocumentApproved> context)
    {
        var msg = context.Message;
        Console.WriteLine($"[Intake] Belge Onaylandı Eventi Alındı: {msg.DocumentId}");

        // 1. Belgeyi Bul
        var document = await _dbContext.Documents.FindAsync(msg.DocumentId);

        if (document != null)
        {
            // 2. Statüyü Güncelle
            document.Status = DocumentStatus.Approved; // Veya Approved (Enum'da ne varsa)

            // Opsiyonel: Onaylanma tarihini de bir yere yazabilirsin
            // document.CompletedAt = msg.ApprovedAt;

            await _dbContext.SaveChangesAsync();
            Console.WriteLine("[Intake] Belge statüsü 'Approved' olarak güncellendi. ✅");
        }
    }
}