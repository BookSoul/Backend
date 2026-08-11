using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IReviewService _reviewService;

    public ProductsController(IProductService productService, IImageStorageService imageStorageService, IReviewService reviewService)
    {
        _productService = productService;
        _imageStorageService = imageStorageService;
        _reviewService = reviewService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(
        [FromQuery] ProductType? type,
        [FromQuery] string? keyword,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? brandId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var items = await _productService.SearchProductsAsync(type, keyword, categoryId, brandId, page, pageSize, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(Guid id, [FromQuery] ProductType type, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, type, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductRequest request, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        string? url = null;
        if (imageFile != null)
        {
            await using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms, cancellationToken);
            url = await _imageStorageService.UploadAsync(imageFile.FileName, imageFile.ContentType, ms.ToArray(), cancellationToken);
        }

        if (request.Type == ProductType.Book)
        {
            if (request.Book != null && url != null) request.Book.ImageUrl = url;
            var book = await _productService.CreateBookAsync(request.Book!, cancellationToken);
            return Ok(book);
        }

        if (request.Accessory != null && url != null) request.Accessory.ImageUrl = url;
        var accessory = await _productService.CreateAccessoryAsync(request.Accessory!, cancellationToken);
        return Ok(accessory);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromQuery] ProductType type, [FromForm] UpdateProductRequest request, IFormFile? imageFile, CancellationToken cancellationToken)
    {
        if (imageFile != null)
        {
            await using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms, cancellationToken);
            var url = await _imageStorageService.UploadAsync(imageFile.FileName, imageFile.ContentType, ms.ToArray(), cancellationToken);
            request.ImageUrl = url;
        }
        var product = await _productService.UpdateProductAsync(id, type, request, cancellationToken);
        return Ok(product);
    }

    public record CreateProductRequest(ProductType Type, CreateBookProductRequest? Book, CreateAccessoryProductRequest? Accessory);

    // ── REVIEW ENDPOINTS ────────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách review của một sản phẩm. type=Book → theo BookId, type=Accessory → theo AccessoryId.
    /// </summary>
    [HttpGet("{id:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviews(
        Guid id,
        [FromQuery] ProductType type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = type == ProductType.Book
            ? await _reviewService.GetByBookIdAsync(id, page, pageSize, cancellationToken)
            : await _reviewService.GetByAccessoryIdAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy tóm tắt điểm đánh giá (average rating, total reviews, phân phối 1-5 sao) của một sản phẩm.
    /// </summary>
    [HttpGet("{id:guid}/reviews/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviewSummary(
        Guid id,
        [FromQuery] ProductType type,
        CancellationToken cancellationToken = default)
    {
        var summary = type == ProductType.Book
            ? await _reviewService.GetRatingSummaryAsync(id, null, cancellationToken)
            : await _reviewService.GetRatingSummaryAsync(null, id, cancellationToken);
        return Ok(summary);
    }
}
