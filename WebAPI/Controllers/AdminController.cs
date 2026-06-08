using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
        => Ok(await _adminService.GetDashboardSummaryAsync(cancellationToken));

    [HttpPost("books")]
    public async Task<IActionResult> CreateBook([FromBody] BookUpsertRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.CreateBookAsync(request, cancellationToken));

    [HttpPut("books/{id:guid}")]
    public async Task<IActionResult> UpdateBook(Guid id, [FromBody] BookUpsertRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpdateBookAsync(id, request, cancellationToken));

    [HttpDelete("books/{id:guid}")]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteBookAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("accessories")]
    public async Task<IActionResult> CreateAccessory([FromBody] AccessoryUpsertRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.CreateAccessoryAsync(request, cancellationToken));

    [HttpPut("accessories/{id:guid}")]
    public async Task<IActionResult> UpdateAccessory(Guid id, [FromBody] AccessoryUpsertRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpdateAccessoryAsync(id, request, cancellationToken));

    [HttpDelete("accessories/{id:guid}")]
    public async Task<IActionResult> DeleteAccessory(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteAccessoryAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
        => Ok(await _adminService.GetOrdersAsync(cancellationToken));

    [HttpPut("orders/{id:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpdateOrderStatusAsync(id, request.Status, cancellationToken));

    [HttpGet("buyback")]
    public async Task<IActionResult> GetBuybacks(CancellationToken cancellationToken)
        => Ok(await _adminService.GetBuybacksAsync(cancellationToken));

    [HttpPut("buyback/{id:guid}/approve")]
    public async Task<IActionResult> ApproveBuyback(Guid id, [FromBody] ApproveBuybackBody body, CancellationToken cancellationToken)
        => Ok(await _adminService.ApproveBuybackAsync(id, body.ApprovedPrice, body.AdminNotes, cancellationToken));

    [HttpPut("buyback/{id:guid}/reject")]
    public async Task<IActionResult> RejectBuyback(Guid id, [FromBody] RejectBuybackBody body, CancellationToken cancellationToken)
        => Ok(await _adminService.RejectBuybackAsync(id, body.Reason, cancellationToken));

    [HttpGet("statistics/charts")]
    public async Task<IActionResult> Charts(CancellationToken cancellationToken)
        => Ok(await _adminService.GetChartDataAsync(cancellationToken));

    [HttpGet("statistics/export")]
    public async Task<IActionResult> Export([FromQuery] string format, CancellationToken cancellationToken)
        => File(await _adminService.ExportStatisticsAsync(format, cancellationToken), "application/octet-stream", $"statistics.{format}");

    public record ApproveBuybackBody(decimal? ApprovedPrice, string? AdminNotes);
    public record RejectBuybackBody(string Reason);
}
