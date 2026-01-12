using System;
using System.Collections.Generic;

namespace IntelliDoc.Modules.Audit.Entities;

public class AuditRecord
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }

    // Hash (Mükerrerlik ve Değişmezlik Kanıtı)
    public string DocumentHash { get; set; } = string.Empty; // SHA-256

    // Model Bilgisi (Provenance)
    public string ModelName { get; set; } = string.Empty; // "gemini-1.5-flash"
    public string ModelVersion { get; set; } = string.Empty; // "v1.0"

    public DateTime ProcessedAt { get; set; }

    // İlişki: Alan Bazlı Tarihçe
    public List<FieldHistory> FieldHistories { get; set; } = new();
}