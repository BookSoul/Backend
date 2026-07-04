using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IImageStorageService _imageStorageService;

    public AdminController(IAdminService adminService, IImageStorageService imageStorageService)
    {
        _adminService = adminService;
        _imageStorageService = imageStorageService;
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
        => Ok(await _adminService.GetDashboardSummaryAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
        => Ok(await _adminService.GetAnalyticsAsync(from, to, cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("book-categories")]
    public async Task<IActionResult> GetBookCategories(CancellationToken cancellationToken)
        => Ok(await _adminService.GetCategoriesAsync(cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("book-categories")]
    public async Task<IActionResult> CreateBookCategory([FromBody] UpsertBookCatalogRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpsertCategoryAsync(null, request.Name, request.Description, cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("book-categories/{id:guid}")]
    public async Task<IActionResult> UpdateBookCategory(Guid id, [FromBody] UpsertBookCatalogRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpsertCategoryAsync(id, request.Name, request.Description, cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("book-categories/{id:guid}")]
    public async Task<IActionResult> DeleteBookCategory(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("book-authors")]
    public async Task<IActionResult> GetBookAuthors(CancellationToken cancellationToken)
        => Ok(await _adminService.GetAuthorsAsync(cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("book-authors")]
    public async Task<IActionResult> CreateBookAuthor([FromBody] UpsertBookCatalogRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpsertAuthorAsync(null, request.Name, request.Description, cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("book-authors/{id:guid}")]
    public async Task<IActionResult> UpdateBookAuthor(Guid id, [FromBody] UpsertBookCatalogRequest request, CancellationToken cancellationToken)
        => Ok(await _adminService.UpsertAuthorAsync(id, request.Name, request.Description, cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("book-authors/{id:guid}")]
    public async Task<IActionResult> DeleteBookAuthor(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteAuthorAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff(CancellationToken cancellationToken)
        => Ok(await _adminService.GetStaffUsersAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromForm] CreateStaffRequest request, IFormFile? avatarFile, CancellationToken cancellationToken)
    {
        if (avatarFile != null)
        {
            await using var ms = new MemoryStream();
            await avatarFile.CopyToAsync(ms, cancellationToken);
            var url = await _imageStorageService.UploadAsync(avatarFile.FileName, avatarFile.ContentType, ms.ToArray(), cancellationToken);
            request = request with { Avatar = url };
        }
        return Ok(await _adminService.CreateStaffAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("staff/{id:guid}")]
    public async Task<IActionResult> UpdateStaff(Guid id, [FromForm] UpdateAdminUserRequest request, IFormFile? avatarFile, CancellationToken cancellationToken)
    {
        if (avatarFile != null)
        {
            await using var ms = new MemoryStream();
            await avatarFile.CopyToAsync(ms, cancellationToken);
            var url = await _imageStorageService.UploadAsync(avatarFile.FileName, avatarFile.ContentType, ms.ToArray(), cancellationToken);
            request = request with { Avatar = url };
        }
        return Ok(await _adminService.UpdateStaffAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("staff/{id:guid}/lock")]
    public async Task<IActionResult> LockStaff(Guid id, CancellationToken cancellationToken)
        => Ok(await _adminService.LockUserAsync(id, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("staff/{id:guid}/unlock")]
    public async Task<IActionResult> UnlockStaff(Guid id, CancellationToken cancellationToken)
        => Ok(await _adminService.UnlockUserAsync(id, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("staff/{id:guid}")]
    public async Task<IActionResult> DeleteStaff(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteStaffAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
        => Ok(await _adminService.GetCustomerUsersAsync(cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromForm] CreateCustomerRequest request, IFormFile? avatarFile, CancellationToken cancellationToken)
    {
        if (avatarFile != null)
        {
            await using var ms = new MemoryStream();
            await avatarFile.CopyToAsync(ms, cancellationToken);
            var url = await _imageStorageService.UploadAsync(avatarFile.FileName, avatarFile.ContentType, ms.ToArray(), cancellationToken);
            request = request with { Avatar = url };
        }
        return Ok(await _adminService.CreateCustomerAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("customers/{id:guid}")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromForm] UpdateAdminUserRequest request, IFormFile? avatarFile, CancellationToken cancellationToken)
    {
        if (avatarFile != null)
        {
            await using var ms = new MemoryStream();
            await avatarFile.CopyToAsync(ms, cancellationToken);
            var url = await _imageStorageService.UploadAsync(avatarFile.FileName, avatarFile.ContentType, ms.ToArray(), cancellationToken);
            request = request with { Avatar = url };
        }
        return Ok(await _adminService.UpdateCustomerAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("customers/{id:guid}/lock")]
    public async Task<IActionResult> LockCustomer(Guid id, CancellationToken cancellationToken)
        => Ok(await _adminService.LockUserAsync(id, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("customers/{id:guid}/unlock")]
    public async Task<IActionResult> UnlockCustomer(Guid id, CancellationToken cancellationToken)
        => Ok(await _adminService.UnlockUserAsync(id, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("customers/{id:guid}")]
    public async Task<IActionResult> DeleteCustomer(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteCustomerAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("books")]
    public async Task<IActionResult> GetBooks(CancellationToken cancellationToken)
        => Ok(await _adminService.GetBooksAsync(cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("books")]
    public async Task<IActionResult> CreateBook([FromForm] BookUpsertRequest request, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        if (imageFile != null)
        {
            await using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms, cancellationToken);
            var url = await _imageStorageService.UploadAsync(imageFile.FileName, imageFile.ContentType, ms.ToArray(), cancellationToken);
            request.ImageUrl = url;
        }
        return Ok(await _adminService.CreateBookAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("books/{id:guid}")]
    public async Task<IActionResult> UpdateBook(Guid id, [FromForm] BookUpsertRequest request, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        if (imageFile != null)
        {
            await using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms, cancellationToken);
            var url = await _imageStorageService.UploadAsync(imageFile.FileName, imageFile.ContentType, ms.ToArray(), cancellationToken);
            request.ImageUrl = url;
        }
        return Ok(await _adminService.UpdateBookAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("books/{id:guid}")]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteBookAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("accessories")]
    public async Task<IActionResult> GetAccessories(CancellationToken cancellationToken)
        => Ok(await _adminService.GetAccessoriesAsync(cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("accessories")]
    public async Task<IActionResult> CreateAccessory([FromForm] AccessoryUpsertRequest request, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        if (imageFile != null)
        {
            await using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms, cancellationToken);
            var url = await _imageStorageService.UploadAsync(imageFile.FileName, imageFile.ContentType, ms.ToArray(), cancellationToken);
            request.ImageUrl = url;
        }
        return Ok(await _adminService.CreateAccessoryAsync(request, cancellationToken));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("accessories/{id:guid}")]
    public async Task<IActionResult> UpdateAccessory(Guid id, [FromForm] AccessoryUpsertRequest request, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        if (imageFile != null)
        {
            await using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms, cancellationToken);
            var url = await _imageStorageService.UploadAsync(imageFile.FileName, imageFile.ContentType, ms.ToArray(), cancellationToken);
            request.ImageUrl = url;
        }
        return Ok(await _adminService.UpdateAccessoryAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("accessories/{id:guid}")]
    public async Task<IActionResult> DeleteAccessory(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.DeleteAccessoryAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Staff,Shipper")]
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
        => Ok(await _adminService.GetOrdersAsync(cancellationToken));

    [Authorize(Roles = "Staff,Shipper")]
    [HttpPut("orders/{id:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var isShipper = User.IsInRole("Shipper");
        var isStaff = User.IsInRole("Staff");

        if (request.Status == OrderStatus.Delivered && !isShipper)
        {
            return BadRequest("Chỉ shipper mới có thể cập nhật trạng thái Đã giao.");
        }

        if (request.Status != OrderStatus.Delivered && !isStaff)
        {
            return BadRequest("Shipper chỉ có thể cập nhật trạng thái Đã giao.");
        }

        return Ok(await _adminService.UpdateOrderStatusAsync(id, request.Status, request.Reason, cancellationToken));
    }

    [Authorize(Roles = "Staff")]
    [HttpPut("orders/{id:guid}/return/approve")]
    public async Task<IActionResult> ApproveReturn(Guid id, [FromBody] ReviewReturnOrderRequest? request, CancellationToken cancellationToken)
        => Ok(await _adminService.ApproveReturnAsync(id, request?.Note, cancellationToken));

    [Authorize(Roles = "Staff")]
    [HttpPut("orders/{id:guid}/return/reject")]
    public async Task<IActionResult> RejectReturn(Guid id, [FromBody] ReviewReturnOrderRequest? request, CancellationToken cancellationToken)
        => Ok(await _adminService.RejectReturnAsync(id, request?.Note, cancellationToken));

    [Authorize(Roles = "Staff")]
    [HttpGet("buyback")]
    public async Task<IActionResult> GetBuybacks(CancellationToken cancellationToken)
        => Ok(await _adminService.GetBuybacksAsync(cancellationToken));

    [Authorize(Roles = "Staff")]
    [HttpPut("buyback/{id:guid}/approve")]
    public async Task<IActionResult> ApproveBuyback(Guid id, [FromBody] ApproveBuybackBody body, CancellationToken cancellationToken)
        => Ok(await _adminService.ApproveBuybackAsync(id, body.ApprovedPrice, body.AdminNotes, cancellationToken));

    [Authorize(Roles = "Staff")]
    [HttpPut("buyback/{id:guid}/reject")]
    public async Task<IActionResult> RejectBuyback(Guid id, [FromBody] RejectBuybackBody body, CancellationToken cancellationToken)
        => Ok(await _adminService.RejectBuybackAsync(id, body.Reason, cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("statistics/charts")]
    public async Task<IActionResult> Charts(CancellationToken cancellationToken)
        => Ok(await _adminService.GetChartDataAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet("statistics/export")]
    public async Task<IActionResult> Export([FromQuery] string format, CancellationToken cancellationToken)
        => File(await _adminService.ExportStatisticsAsync(format, cancellationToken), "application/octet-stream", $"statistics.{format}");

    public record ApproveBuybackBody(decimal? ApprovedPrice, string? AdminNotes);
    public record RejectBuybackBody(string Reason);
    public record UpsertBookCatalogRequest(string Name, string? Description);
}
