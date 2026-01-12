using IntelliDoc.Modules.Extraction.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntelliDoc.Modules.Extraction.Endpoints;

[ApiController]
[Route("api/extraction")]
public class ExtractionController : ControllerBase
{
    private readonly ExtractionDbContext _dbContext;

    public ExtractionController(ExtractionDbContext dbContext)
    {
        _dbContext = dbContext;
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
}