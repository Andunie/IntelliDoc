using System;

namespace IntelliDoc.Modules.Audit.Entities;

public class FieldHistory
{
    public Guid Id { get; set; }
    public Guid AuditRecordId { get; set; } // Hangi Audit kaydına ait?

    public string FieldName { get; set; } = string.Empty; // Örn: "TotalAmount"
    public string? OldValue { get; set; } // Örn: "100.00" (veya null)
    public string? NewValue { get; set; } // Örn: "100.50"

    public double ConfidenceScore { get; set; } // 0.95
    public string ChangedBy { get; set; } = "SYSTEM"; // "SYSTEM" veya "user-123"
    public string? ChangeReason { get; set; } // "OCR Correction"

    public DateTime ChangedAt { get; set; }
}