using Application.Interface;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/home")]
public class HomeController : ControllerBase
{
    private readonly IHomeService _homeService;

    public HomeController(IHomeService homeService)
    {
        _homeService = homeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHome(CancellationToken cancellationToken)
    {
        var result = await _homeService.GetHomePageAsync(cancellationToken);
        return Ok(result);
    }
}
