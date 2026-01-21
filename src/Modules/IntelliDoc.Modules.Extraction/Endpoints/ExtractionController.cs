using IntelliDoc.Modules.Extraction.Data;
using IntelliDoc.Modules.Extraction.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntelliDoc.Modules.Extraction.Endpoints;

[ApiController]
[Route("api/extraction")]
public class ExtractionController : ControllerBase
{
    private readonly ExtractionDbContext _dbContext;
    private readonly ExcelService _excelService;

    public ExtractionController(ExtractionDbContext dbContext, ExcelService excelService)
    {
        _dbContext = dbContext;
        _excelService = excelService;
    }

    // Belge ID'sine göre çıkarılan veriyi getir
    [HttpGet("{documentId}")]
    public async Task<IActionResult> GetResult(Guid documentId)
    {
        var result = await _dbContext.ExtractionResults
            .FirstOrDefaultAsync(x => x.DocumentId == documentId);

        if (result == null)
            return NotFound("Bu belge için henüz analiz sonucu yok veya işlem devam ediyor.");

        return Ok(result);
    }

    [HttpGet("{documentId}/export")]
    public async Task<IActionResult> ExportToExcel(Guid documentId)
    {
        var result = await _dbContext.ExtractionResults.FirstOrDefaultAsync(x => x.DocumentId == documentId);
        if (result == null) return NotFound("Veri yok.");

        var fileBytes = _excelService.GenerateExcel(result.JsonData);
        var fileName = $"Export_{documentId}.xlsx";

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("export-batch")]
    public async Task<IActionResult> ExportBatch([FromBody] List<Guid> documentIds)
    {
        if (documentIds == null || !documentIds.Any())
            return BadRequest("Lütfen en az bir belge seçin.");

        // Veritabanından seçili belgelerin JSON'larını çek
        var results = await _dbContext.ExtractionResults
            .Where(x => documentIds.Contains(x.DocumentId))
            .Select(x => x.JsonData)
            .ToListAsync();

        if (!results.Any()) return NotFound("Seçilen belgeler bulunamadı.");

        var fileBytes = _excelService.GenerateBatchExcel(results);
        var fileName = $"Toplu_Rapor_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}