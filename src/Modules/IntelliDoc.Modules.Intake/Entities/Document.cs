using System;

namespace IntelliDoc.Modules.Intake.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty; // Örn: Fatura_2024.pdf
    public string ContentType { get; set; } = string.Empty;      // Örn: application/pdf
    public string StoragePath { get; set; } = string.Empty;      // MinIO'daki yol (2024/01/guid.pdf)
    public long FileSize { get; set; }                           // Byte cinsinden boyut
    public DocumentStatus Status { get; set; }                   // İşlem durumu
    public DateTime UploadedAt { get; set; }
}

public enum DocumentStatus
{
    Uploaded,           // Yüklendi, bekliyor
    Processing,         // AI işliyor
    Completed,          // Başarıyla bitti
    Failed              // Hata aldı
}