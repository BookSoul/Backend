using Application.DTO;
using Application.Interface;
using Domain.Entities.Identity;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ICheckoutService _checkoutService;
    private readonly IBuybackService _buybackService;
    private readonly UserManager<User> _userManager;
    private readonly IImageStorageService _imageStorageService;

    public UserController(ICartService cartService, ICheckoutService checkoutService, IBuybackService buybackService, UserManager<User> userManager, IImageStorageService imageStorageService)
    {
        _cartService = cartService;
        _checkoutService = checkoutService;
        _buybackService = buybackService;
        _userManager = userManager;
        _imageStorageService = imageStorageService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(ToProfileDto(user, roles));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateUserProfileRequest request, IFormFile? avatarFile, CancellationToken cancellationToken)
    {
        string? avatarUrl = request.Avatar;
        if (avatarFile != null)
        {
            await using var ms = new MemoryStream();
            await avatarFile.CopyToAsync(ms, cancellationToken);
            avatarUrl = await _imageStorageService.UploadAsync(avatarFile.FileName, avatarFile.ContentType, ms.ToArray(), cancellationToken);
        }

        var fullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Name : request.FullName;
        var userName = string.IsNullOrWhiteSpace(request.UserName) ? request.Name : request.UserName;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return BadRequest(new { message = "Họ tên không được để trống." });
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            return BadRequest(new { message = "Username không được để trống." });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email không được để trống." });
        }

        var user = await _userManager.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return Unauthorized();

        var normalizedEmail = request.Email.Trim();
        var emailOwner = await _userManager.FindByEmailAsync(normalizedEmail);
        if (emailOwner is not null && emailOwner.Id != user.Id)
        {
            return BadRequest(new { message = "Email này đã được sử dụng bởi tài khoản khác." });
        }

        var normalizedUserName = userName.Trim();
        var userNameOwner = await _userManager.FindByNameAsync(normalizedUserName);
        if (userNameOwner is not null && userNameOwner.Id != user.Id)
        {
            return BadRequest(new { message = "Username này đã được sử dụng bởi tài khoản khác." });
        }

        user.FullName = fullName.Trim();
        user.Email = normalizedEmail;
        user.UserName = normalizedUserName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(ToProfileDto(user, roles));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return BadRequest(new { message = "Vui lòng nhập mật khẩu hiện tại." });
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });
        }

        var user = await _userManager.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var hasPasswordMismatch = result.Errors.Any(e =>
                e.Code.Equals("PasswordMismatch", StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains("Incorrect password", StringComparison.OrdinalIgnoreCase));

            if (hasPasswordMismatch)
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });
            }

            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        return NoContent();
    }

    [HttpGet("cart")]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken) => Ok(await _cartService.GetCartAsync(User.GetUserId(), cancellationToken));

    [HttpPost("cart")]
    public async Task<IActionResult> AddToCart([FromBody] AddCartItemBody body, CancellationToken cancellationToken)
        => Ok(await _cartService.AddItemAsync(User.GetUserId(), body.ProductId, body.ProductType, body.Quantity, cancellationToken));

    [HttpPut("cart/{itemId:guid}")]
    public async Task<IActionResult> UpdateCartItem(Guid itemId, [FromBody] UpdateCartBody body, CancellationToken cancellationToken)
        => Ok(await _cartService.UpdateItemAsync(User.GetUserId(), body.ProductId, body.ProductType, body.Quantity, cancellationToken));

    // Order endpoints are exposed via OrdersController under /api/user/orders

    [HttpPost("buyback/regular")]
    public async Task<IActionResult> RegularBuyback([FromBody] BuybackRegularBody body, CancellationToken cancellationToken)
        => Ok(await _buybackService.CreateRequestAsync(
            User.GetUserId(),
            new CreateBuybackRequest(
                BuybackType.Regular,
                body.ProposedPrice,
                body.BookTitle,
                body.Author,
                body.Category,
                body.Condition,
                body.PublishYear,
                body.Description,
                null,
                null,
                null,
                body.BuybackPrice,
                body.OriginalPrice,
                body.Reason,
                body.UserName,
                body.UserEmail,
                body.UserPhone,
                body.UserAddress),
            [],
            cancellationToken));

    [HttpPost("buyback/blindbox")]
    public async Task<IActionResult> BlindBoxBuyback([FromBody] BuybackBlindBoxBody body, CancellationToken cancellationToken)
        => Ok(await _buybackService.CreateRequestAsync(
            User.GetUserId(),
            new CreateBuybackRequest(
                BuybackType.BlindBox,
                body.ProposedPrice,
                null,
                null,
                null,
                null,
                null,
                null,
                body.OrderId,
                body.BlindBoxTier,
                body.BlindBoxCategory,
                body.BuybackPrice,
                body.OriginalPrice,
                body.Reason,
                body.UserName,
                body.UserEmail,
                body.UserPhone,
                body.UserAddress),
            [],
            cancellationToken));

    public record AddCartItemBody(Guid ProductId, ProductType ProductType, int Quantity);
    public record UpdateCartBody(Guid ProductId, ProductType ProductType, int Quantity);
    public record BuybackRegularBody(
        decimal ProposedPrice,
        string? BookTitle,
        string? Author,
        string? Category,
        string? Condition,
        string? PublishYear,
        string? Description,
        decimal? BuybackPrice,
        decimal? OriginalPrice,
        string? Reason,
        string? UserName,
        string? UserEmail,
        string? UserPhone,
        string? UserAddress);
    public record BuybackBlindBoxBody(
        decimal ProposedPrice,
        string? OrderId,
        string? BlindBoxTier,
        string? BlindBoxCategory,
        decimal? BuybackPrice,
        decimal? OriginalPrice,
        string? Reason,
        string? UserName,
        string? UserEmail,
        string? UserPhone,
        string? UserAddress);

    private static UserProfileDto ToProfileDto(User user, IList<string> roles)
    {
        var role = roles.Contains("Admin")
            ? "admin"
            : roles.Contains("Shipper")
                ? "shipper"
                : roles.Contains("Staff")
                    ? "staff"
                    : "user";

        return new UserProfileDto(
            user.Id.ToString(),
            user.UserName ?? user.FullName,
            user.Email ?? string.Empty,
            user.AvatarUrl,
            role,
            user.Address,
            user.PhoneNumber,
            user.FullName,
            user.UserName);
    }
}
