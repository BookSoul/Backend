using Application.DTO;
using Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/user/donate")]
public class DonateController : ControllerBase
{
    private readonly IDonateService _donateService;

    public DonateController(IDonateService donateService)
    {
        _donateService = donateService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDonateRequest([FromBody] CreateDonateRequest request, CancellationToken cancellationToken)
        => Ok(await _donateService.CreateAsync(User.GetUserId(), request, cancellationToken));
}
