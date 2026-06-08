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

    public ProductsController(IProductService productService)
    {
        _productService = productService;
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
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (request.Type == ProductType.Book)
        {
            var book = await _productService.CreateBookAsync(request.Book!, cancellationToken);
            return Ok(book);
        }

        var accessory = await _productService.CreateAccessoryAsync(request.Accessory!, cancellationToken);
        return Ok(accessory);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromQuery] ProductType type, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _productService.UpdateProductAsync(id, type, request, cancellationToken);
        return Ok(product);
    }

    public record CreateProductRequest(ProductType Type, CreateBookProductRequest? Book, CreateAccessoryProductRequest? Accessory);
}
