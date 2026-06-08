using Application.DTO;
using Application.Interface;
using Domain.Entities.Books;
using Domain.Entities.Orders;
using Domain.Entities.System;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Infrastructure.Service;

public class CheckoutService : ICheckoutService
{
    private readonly AppDbContext _context;
    private readonly ICartService _cartService;
    private readonly IUnitOfWork _unitOfWork;

    public CheckoutService(AppDbContext context, ICartService cartService, IUnitOfWork unitOfWork)
    {
        _context = context;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CheckoutResponseDto> CreateOrderAsync(Guid customerId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await _cartService.GetCartAsync(customerId, cancellationToken);
        var blindBoxLines = request.BlindBoxLines ?? Array.Empty<BlindBoxOrderLine>();

        if (cart.Items.Count == 0 && blindBoxLines.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var subTotal = cart.SubTotal + blindBoxLines.Sum(x => x.UnitPrice * x.Quantity);
            Voucher? voucher = null;
            decimal discountAmount = 0;
            string? appliedCode = null;

            if (!string.IsNullOrWhiteSpace(request.VoucherCode))
            {
                var normalized = request.VoucherCode.Trim().ToUpperInvariant();
                voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == normalized, cancellationToken);
                if (voucher is null || !voucher.IsActive || voucher.ExpiryDate < DateTime.UtcNow)
                {
                    throw new InvalidOperationException("Invalid or expired voucher.");
                }

                if (subTotal < voucher.MinOrderValue)
                {
                    throw new InvalidOperationException($"Order must be at least {voucher.MinOrderValue} to use this voucher.");
                }

                discountAmount = Math.Min(voucher.DiscountAmount, subTotal);
                appliedCode = voucher.Code;
            }

            var shippingFee = await GetShippingFeeAsync(cancellationToken);
            var finalTotal = Math.Max(subTotal - discountAmount, 0) + shippingFee;
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                ReceiverName = request.ReceiverName,
                ReceiverPhone = request.ReceiverPhone,
                ReceiverEmail = request.ReceiverEmail,
                ShippingAddress = request.ShippingAddress,
                Notes = request.Notes,
                Status = OrderStatus.Pending,
                PaymentMethod = request.PaymentMethod,
                ShippingFee = shippingFee,
                Discount = discountAmount,
                TotalAmount = finalTotal
            };

            foreach (var item in cart.Items)
            {
                await AddOrderLineFromCartAsync(order, item, cancellationToken);
            }

            foreach (var blind in blindBoxLines)
            {
                var pickedBook = await PickRandomBlindBoxBookAsync(cancellationToken);
                if (pickedBook is null)
                {
                    throw new InvalidOperationException("No eligible book available for blind box.");
                }

                if (pickedBook.Stock < blind.Quantity)
                {
                    throw new InvalidOperationException("Not enough stock for blind box assignment.");
                }

                pickedBook.Stock -= blind.Quantity;
                order.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    BookId = pickedBook.Id,
                    AccessoryId = null,
                    BlindBoxTier = BlindBoxTier.Normal,
                    BlindBoxGenre = pickedBook.Category?.Name,
                    Price = blind.UnitPrice,
                    Quantity = blind.Quantity
                });
            }

            _context.Orders.Add(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            await _cartService.ClearCartAsync(customerId, cancellationToken);

            return new CheckoutResponseDto(
                order.Id,
                subTotal,
                shippingFee,
                discountAmount,
                finalTotal,
                order.Status.ToString(),
                appliedCode);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task AddOrderLineFromCartAsync(Order order, CartItemDto item, CancellationToken cancellationToken)
    {
        if (item.ProductType == ProductType.Book)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == item.ProductId, cancellationToken)
                ?? throw new KeyNotFoundException($"Book {item.ProductId} not found.");

            if (book.Stock < item.Quantity)
            {
                throw new InvalidOperationException($"Not enough stock for '{book.Title}'.");
            }

            book.Stock -= item.Quantity;
            order.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                BookId = book.Id,
                AccessoryId = null,
                Quantity = item.Quantity,
                Price = book.Price,
                BlindBoxTier = null,
                BlindBoxGenre = null
            });
        }
        else
        {
            var accessory = await _context.Accessories.FirstOrDefaultAsync(a => a.Id == item.ProductId, cancellationToken)
                ?? throw new KeyNotFoundException($"Accessory {item.ProductId} not found.");

            if (accessory.Stock < item.Quantity)
            {
                throw new InvalidOperationException($"Not enough stock for '{accessory.Name}'.");
            }

            accessory.Stock -= item.Quantity;
            order.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                BookId = null,
                AccessoryId = accessory.Id,
                Quantity = item.Quantity,
                Price = accessory.Price,
                BlindBoxTier = null,
                BlindBoxGenre = null
            });
        }
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetMyOrdersAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        return orders.Select(MapOrder).ToList();
    }

    public async Task<OrderSummaryDto> GetOrderDetailAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        return await _cartService.GetCartAsync(customerId, cancellationToken)
            .ContinueWith(_ => MapOrder(order), cancellationToken);
    }

    public async Task<OrderSummaryDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        if (status == OrderStatus.Cancelled && order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("Only pending orders can be cancelled.");
        }

        order.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task<OrderSummaryDto> ReorderAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");


        var cartItems = new List<CartItemDto>();
        foreach (var item in order.OrderItems)
        {
            if (item.BookId.HasValue)
            {
                var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == item.BookId.Value, cancellationToken);
                if (book is null) continue;
                cartItems.Add(new CartItemDto(book.Id, ProductType.Book, book.Title, item.Quantity, book.Price, book.Price * item.Quantity, false));
            }
            else if (item.AccessoryId.HasValue)
            {
                var accessory = await _context.Accessories.AsNoTracking().FirstOrDefaultAsync(a => a.Id == item.AccessoryId.Value, cancellationToken);
                if (accessory is null) continue;
                cartItems.Add(new CartItemDto(accessory.Id, ProductType.Accessory, accessory.Name, item.Quantity, accessory.Price, accessory.Price * item.Quantity, false));
            }
        }

        foreach (var cartItem in cartItems)
        {
            await _cartService.AddItemAsync(customerId, cartItem.ProductId, cartItem.ProductType, cartItem.Quantity, cancellationToken);
        }

        return MapOrder(order);
    }

    public Task<OrderSummaryDto> AssignBlindBoxProductAsync(Guid orderId, AssignBlindBoxProductRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Blind box assignment is handled automatically during checkout in the current domain model.");

    private async Task<Book?> PickRandomBlindBoxBookAsync(CancellationToken cancellationToken)
    {
        var books = await _context.Books
            .Include(b => b.Category)
            .Where(b => b.IsActive && b.Stock > 0)
            .OrderBy(b => Guid.NewGuid())
            .ToListAsync(cancellationToken);

        return books.FirstOrDefault();
    }

    private async Task<decimal> GetShippingFeeAsync(CancellationToken cancellationToken)
    {
        var row = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return row?.ShippingFee ?? 0m;
    }

    private static OrderSummaryDto MapOrder(Order order) => new(
        order.Id,
        order.OrderDate,
        order.TotalAmount,
        order.Status.ToString(),
        order.PaymentMethod.ToString(),
        order.OrderItems.Select(oi => new OrderItemDto(
            oi.BookId ?? oi.AccessoryId,
            oi.BookId.HasValue ? ProductType.Book : ProductType.Accessory,
            oi.BlindBoxGenre ?? (oi.BookId.HasValue ? "Book" : "Accessory"),
            oi.Quantity,
            oi.Price,
            oi.BlindBoxTier.HasValue)).ToList());
}
