using Application.DTO;
using Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/import-tickets")]
public class ImportTicketsController : ControllerBase
{
    private readonly IImportTicketService _importTicketService;

    public ImportTicketsController(IImportTicketService importTicketService)
    {
        _importTicketService = importTicketService;
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateImportTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await _importTicketService.CreateTicketAsync(User.GetUserId(), request, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetTickets([FromQuery] bool mineOnly = false, CancellationToken cancellationToken = default)
    {
        var staffId = mineOnly ? User.GetUserId() : (Guid?)null;
        var result = await _importTicketService.GetTicketsAsync(staffId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/submit")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var result = await _importTicketService.SubmitTicketAsync(id, User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _importTicketService.ApproveTicketAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectImportTicketRequest? request, CancellationToken cancellationToken)
    {
        var result = await _importTicketService.RejectTicketAsync(id, request?.Note, cancellationToken);
        return Ok(result);
    }

    public record RejectImportTicketRequest(string? Note);
}
