using Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;

    public UploadController(IImageStorageService imageStorageService)
    {
        _imageStorageService = imageStorageService;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var url = await _imageStorageService.UploadAsync(file.FileName, file.ContentType, ms.ToArray(), cancellationToken);
        return Ok(new { Url = url });
    }
}
