using IntelliDoc.Modules.Search.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var results = await _searchService.SearchAsync(q, userId);
        return Ok(results);
    }
}