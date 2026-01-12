using IntelliDoc.Modules.Search.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntelliDoc.Modules.Search.Endpoints;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly SearchService _searchService;

    public SearchController(SearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Arama terimi giriniz.");

        var results = await _searchService.SearchAsync(q);
        return Ok(results);
    }
}