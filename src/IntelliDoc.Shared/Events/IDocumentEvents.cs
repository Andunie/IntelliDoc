using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelliDoc.Shared.Events;

// 1. Intake Modülü -> Dosya yüklendiğinde fırlatır
public interface IDocumentUploaded
{
    Guid DocumentId { get; }
    string FileName { get; }
    string FilePath { get; } // MinIO path
    string UploadedBy { get; } // Kullanıcı ID
    DateTime UploadedAt { get; }
}

// 2. Extraction Modülü -> OCR işlemi bittiğinde fırlatır
public interface IDataExtracted
{
    Guid DocumentId { get; }
    string RawText { get; } // Ham metin (Search için)
    string JsonData { get; } // Yapılandırılmış veri (Fatura No, Tutar vb.)
    bool Success { get; }
    string? ErrorMessage { get; }
}

// 3. Audit Modülü -> Denetim bittiğinde fırlatır
public interface IDocumentAudited
{
    Guid DocumentId { get; }
    bool IsApproved { get; }
    string AuditRemarks { get; } // "Mükerrer kayıt tespit edildi" vb.
}