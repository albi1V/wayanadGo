using Microsoft.AspNetCore.Mvc;
using WayanadGo.Application.Services;

namespace WayanadGo.Api.Controllers;

[ApiController]
[Route("api/destinations")]
public sealed class DestinationsController(DestinationService service) : ControllerBase
{
    // GET /api/destinations?search=waterfall
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.SearchAsync(search, ct));

    // GET /api/destinations/12
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var destination = await service.GetPublishedByIdAsync(id, ct);
        return destination is null ? NotFound() : Ok(destination);
    }
}
