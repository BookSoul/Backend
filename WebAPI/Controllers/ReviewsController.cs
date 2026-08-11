using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/user/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Tạo review mới cho một sản phẩm (phải có đơn hàng Delivered chứa sản phẩm đó).</summary>
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
        => Ok(await _reviewService.CreateAsync(User.GetUserId(), request, cancellationToken));

    /// <summary>Cập nhật rating và comment của review (chỉ owner).</summary>
    [HttpPut("{reviewId:guid}")]
    public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] UpdateReviewRequest request, CancellationToken cancellationToken)
        => Ok(await _reviewService.UpdateAsync(User.GetUserId(), reviewId, request, cancellationToken));

    /// <summary>Xóa review của chính mình (chỉ owner).</summary>
    [HttpDelete("{reviewId:guid}")]
    public async Task<IActionResult> DeleteReview(Guid reviewId, CancellationToken cancellationToken)
    {
        await _reviewService.DeleteAsync(User.GetUserId(), reviewId, cancellationToken);
        return NoContent();
    }

    /// <summary>Lấy danh sách tất cả reviews của người dùng hiện tại (có phân trang).</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => Ok(await _reviewService.GetMyReviewsAsync(User.GetUserId(), page, pageSize, cancellationToken));

    /// <summary>Kiểm tra xem người dùng có quyền đánh giá sản phẩm hay không.</summary>
    [HttpGet("eligibility")]
    public async Task<IActionResult> CheckEligibility(
        [FromQuery] Guid? bookId,
        [FromQuery] Guid? accessoryId,
        CancellationToken cancellationToken = default)
        => Ok(await _reviewService.CheckEligibilityAsync(User.GetUserId(), bookId, accessoryId, cancellationToken));
}
