using Application.Interface;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/home")]
public class HomeController : ControllerBase
{
    private readonly IHomeService _homeService;
    private readonly AppDbContext _context;

    public HomeController(IHomeService homeService, AppDbContext context)
    {
        _homeService = homeService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetHome(CancellationToken cancellationToken)
    {
        var result = await _homeService.GetHomePageAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("settings/shipping-fee")]
    public async Task<IActionResult> GetShippingFee(CancellationToken cancellationToken)
    {
        var setting = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return Ok(new { shippingFee = setting?.ShippingFee ?? 0m });
    }
}
