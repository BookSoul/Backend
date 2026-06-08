using Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IAdminService _adminService;

    public CategoriesController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] UpsertCatalogRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpsertCategoryAsync(null, request.Name, request.Description, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand([FromBody] UpsertCatalogRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpsertBrandAsync(null, request.Name, request.Description, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("brands/{id:guid}")]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteBrandAsync(id, cancellationToken);
        return NoContent();
    }

    public record UpsertCatalogRequest(string Name, string? Description);
}
