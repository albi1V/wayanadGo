using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WayanadGo.Application.Services;
using WayanadGo.Contracts.Destinations;

namespace WayanadGo.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/destinations")]
public sealed class AdminDestinationsController(DestinationService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateDestinationRequest request, CancellationToken ct)
    {
        try { return Created("/api/admin/destinations", await service.CreateAsync(request, ct)); }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Conflict(new { message = "A destination with this slug may already exist." });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateDestinationRequest request, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await service.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
