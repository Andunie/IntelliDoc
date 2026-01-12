using System;

namespace IntelliDoc.Modules.Audit.Entities;

public class BusinessRuleLog
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }

    public string RuleName { get; set; } // "HighAmountLimit"
    public bool IsPassed { get; set; }
    public string? Message { get; set; }

    public DateTime CheckedAt { get; set; }
}