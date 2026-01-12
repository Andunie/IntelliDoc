using IntelliDoc.Modules.Audit.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntelliDoc.Modules.Audit.Endpoints;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly AuditDbContext _dbContext;

    public AuditController(AuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Belgenin tüm tarihçesini getir
    [HttpGet("history/{documentId}")]
    public async Task<IActionResult> GetHistory(Guid documentId)
    {
        var record = await _dbContext.AuditRecords
            .Include(x => x.FieldHistories)
            .FirstOrDefaultAsync(x => x.DocumentId == documentId);

        if (record == null) return NotFound("Bu belge için denetim kaydı yok.");

        return Ok(record);
    }

    // İnsan Müdahalesi (Human-in-the-Loop)
    // Frontend'de kullanıcı bir alanı düzelttiğinde bu çağrılır
    [HttpPost("update-field")]
    public async Task<IActionResult> UpdateField([FromBody] UpdateFieldRequest request)
    {
        var record = await _dbContext.AuditRecords
            .FirstOrDefaultAsync(x => x.DocumentId == request.DocumentId);

        if (record == null) return NotFound();

        var history = new IntelliDoc.Modules.Audit.Entities.FieldHistory
        {
            Id = Guid.NewGuid(),
            AuditRecordId = record.Id,
            FieldName = request.FieldName,
            OldValue = request.OldValue,
            NewValue = request.NewValue,
            ChangedBy = request.UserId, // "user-123"
            ChangeReason = request.Reason,
            ChangedAt = DateTime.UtcNow,
            ConfidenceScore = 1.0 // İnsan girdisi %100 güvendir
        };

        _dbContext.FieldHistories.Add(history);
        await _dbContext.SaveChangesAsync();

        return Ok(new { Message = "Değişiklik kaydedildi.", HistoryId = history.Id });
    }
}

public class UpdateFieldRequest
{
    public Guid DocumentId { get; set; }
    public string FieldName { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public string Reason { get; set; }
    public string UserId { get; set; }
}