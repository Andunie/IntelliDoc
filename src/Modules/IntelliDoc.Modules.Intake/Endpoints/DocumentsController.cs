using IntelliDoc.Modules.Intake.Data;
using IntelliDoc.Modules.Intake.Entities;
using IntelliDoc.Modules.Intake.Services;
using IntelliDoc.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Intake.Endpoints;

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
        if (file == null || file.Length == 0)
            return BadRequest("Lütfen geçerli bir dosya yükleyin.");

        // 1. MinIO'ya yükle
        using var stream = file.OpenReadStream();
        var storagePath = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        // 2. Veritabanına kaydet
        var document = new Document
        {
            Id = Guid.NewGuid(),
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
            UploadedAt = document.UploadedAt
        });

        return Ok(new { Message = "Dosya başarıyla yüklendi.", DocumentId = document.Id });
    }
}