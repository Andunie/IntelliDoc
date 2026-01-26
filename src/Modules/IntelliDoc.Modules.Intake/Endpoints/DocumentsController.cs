using IntelliDoc.Modules.Intake.Data;
using IntelliDoc.Modules.Intake.Entities;
using IntelliDoc.Modules.Intake.Services;
using IntelliDoc.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Intake.Endpoints;

[Authorize]
[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly MinioStorageService _storageService;
    private readonly IntakeDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public DocumentsController( MinioStorageService storageService, IntakeDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _storageService = storageService;
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        // 1. Kullanıcıyı Bul
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Token'dan okur
        // Veya "sub" claim'i: User.FindFirst("sub")?.Value

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Kimlik doğrulanamadı.");

        if (file == null || file.Length == 0)
            return BadRequest("Lütfen geçerli bir dosya yükleyin.");

        // 1. MinIO'ya yükle
        using var stream = file.OpenReadStream();
        var storagePath = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        // 2. Veritabanına kaydet
        var document = new Document
        {
            Id = Guid.NewGuid(),
            UploadedBy = userId,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            StoragePath = storagePath,
            Status = DocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync();

        // 3. Event fırlat
        await _publishEndpoint.Publish<IDocumentUploaded>(new
        {
            DocumentId = document.Id,
            FileName = document.OriginalFileName,
            FilePath = document.StoragePath,
            UploadedBy = document.UploadedBy,
            UploadedAt = document.UploadedAt
        });

        return Ok(new { Message = "Dosya başarıyla yüklendi.", DocumentId = document.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var documents = _dbContext.Documents
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id,
                d.OriginalFileName,
                d.Status,
                d.UploadedAt,
                d.UploadedBy
            })
            .ToList();

        return Ok(documents);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        // 1. Token'dan Kullanıcı ID'sini al (Upload metoduyla aynı mantık)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Kullanıcı kimliği bulunamadı.");

        // 2. Sadece bu kullanıcının belgelerini getir
        var documents = _dbContext.Documents
            .Where(d => d.UploadedBy == userId) // <--- KRİTİK FİLTRE
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id,
                d.OriginalFileName,
                d.Status, // Enum değeri (0, 1, 2...) döner
                d.UploadedAt,
                d.UploadedBy
            })
            .ToList();

        return Ok(documents);
    }

    // Tekil Belgeyi Getir (Opsiyonel ama Workbench için iyi olabilir)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var document = await _dbContext.Documents.FindAsync(id);
        if (document == null) return NotFound("Belge bulunamadı.");

        return Ok(new
        {
            document.Id,
            document.OriginalFileName,
            document.Status,
            document.UploadedAt,
            document.UploadedBy,
            document.StoragePath
        });
    }

    [HttpGet("{id}/download-url")]
    public async Task<IActionResult> GetDownloadUrl(Guid id)
    {
        var document = await _dbContext.Documents.FindAsync(id);
        if (document == null) return NotFound("Belge bulunamadı.");
        // MinIO servisinde bu metodun (GetPresignedUrlAsync) olması gerekir.
        // Dosya adını (StoragePath) verip, 1 saatlik geçici bir URL alacağız.
        var url = await _storageService.GetPresignedUrlAsync(document.StoragePath);

        // Eğer MinIO servisinde henüz bu metot yoksa, MinIO Client'ının "PresignedGetObjectAsync" metodunu kullanabilirsin.
        return Ok(new { Url = url });
    }
}