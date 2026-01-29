using IntelliDoc.Modules.Extraction.Data;
using IntelliDoc.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace IntelliDoc.Modules.Extraction.Consumers;

public class ApproveDocumentConsumer : IConsumer<IDocumentApprovalRequested>
{
    private readonly ExtractionDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public ApproveDocumentConsumer(ExtractionDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<IDocumentApprovalRequested> context)
    {
        var msg = context.Message;
        Console.WriteLine($"[Extraction] Onay İsteği Geldi: {msg.DocumentId}");

        // Veriyi bul
        var result = await _dbContext.ExtractionResults
            .FirstOrDefaultAsync(x => x.DocumentId == msg.DocumentId);

        if (result != null)
        {
            // İŞTE BURADA ASIL "ONAYLANDI" EVENTİNİ FIRLATIYORUZ 🚀
            // Ve içine JSON verisini koyuyoruz.
            await _publishEndpoint.Publish<IDocumentApproved>(new
            {
                DocumentId = msg.DocumentId,
                UserId = msg.UserId,
                ApprovedAt = DateTime.UtcNow,
                FinalJsonData = result.JsonData // Dolu Dolu Veri!
            });

            Console.WriteLine("[Extraction] Belge onaylandı ve tüm sisteme duyuruldu. ✅");
        }
    }
}