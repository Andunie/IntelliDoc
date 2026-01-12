using System;

namespace IntelliDoc.Modules.Extraction.Entities;

public class ExtractionResult
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }     // Hangi döküman?
    public string? JsonData { get; set; }     // AI'dan dönen yapısal veri
    public string? RawText { get; set; }      // Ham metin (Search için)
    public double ConfidenceScore { get; set; } // Güven skoru
    public DateTime ProcessedAt { get; set; }
}