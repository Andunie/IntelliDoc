using IntelliDoc.Modules.Audit.Data;
using IntelliDoc.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntelliDoc.Modules.Audit.Endpoints;

[Authorize]
[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly AuditDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuditController(AuditDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
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

        await _publishEndpoint.Publish<IFieldUpdated>(new
        {
            DocumentId = request.DocumentId,
            FieldName = request.FieldName,
            NewValue = request.NewValue,
            UpdatedBy = request.UserId,
            UpdatedAt = DateTime.UtcNow
        });


        return Ok(new { Message = "Değişiklik kaydedildi.", HistoryId = history.Id });
    }

    [HttpPost("approve/{documentId}")]
    public async Task<IActionResult> ApproveDocument(Guid documentId)
    {
        // 1. Audit kaydını bul
        var record = await _dbContext.AuditRecords.FirstOrDefaultAsync(x => x.DocumentId == documentId);
        if (record == null) return NotFound();

        // 2. Durumu güncelle (Eğer AuditRecord'da Status yoksa, BusinessRuleLog atabiliriz veya Status alanı ekleyebiliriz)
        // Şimdilik sadece Event fırlatalım, en temizi.

        // 3. Sisteme Haber Ver: "Bu belge onaylandı!"
        await _publishEndpoint.Publish<IDocumentApproved>(new
        {
            DocumentId = documentId,
            ApprovedBy = "CurrentUser", // User.Identity.Name
            ApprovedAt = DateTime.UtcNow
        });

        return Ok(new { Message = "Belge onaylandı." });
    }

    [HttpGet("logs")]
    [Authorize] // Sadece giriş yapanlar
    public async Task<IActionResult> GetAllLogs()
    {
        // 1. Giriş yapan kullanıcının ID'sini al
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        // Veya token yapına göre: User.FindFirst("sub")?.Value

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // 2. Sadece BU kullanıcının yaptığı değişiklikleri getir
        var logs = await _dbContext.FieldHistories
            .Include(h => h.AuditRecord)
            .Where(h => h.ChangedBy == userId) // <--- FİLTRE BURADA
            .OrderByDescending(h => h.ChangedAt)
            .Take(100)
            .Select(h => new
            {
                Id = h.Id,
                Timestamp = h.ChangedAt,
                User = h.ChangedBy,
                Action = "Update Field",
                Details = $"{h.FieldName}: {h.OldValue} -> {h.NewValue}",
                Reference = h.AuditRecord.DocumentId,
                Reason = h.ChangeReason
            })
            .ToListAsync();

        return Ok(logs);
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