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
    private readonly IVnPayService _vnPayService;
    private readonly INotificationService _notificationService;

    public CheckoutService(AppDbContext context, ICartService cartService, IUnitOfWork unitOfWork, IVnPayService vnPayService, INotificationService notificationService)
    {
        _context = context;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
        _vnPayService = vnPayService;
        _notificationService = notificationService;
    }

    public async Task<CheckoutResponseDto> CreateOrderAsync(Guid customerId, CreateOrderRequest request, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => CreateOrderCoreAsync(customerId, request, clientIp, cancellationToken));
    }

    private async Task<CheckoutResponseDto> CreateOrderCoreAsync(Guid customerId, CreateOrderRequest request, string? clientIp, CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetCartAsync(customerId, cancellationToken);
        var blindBoxLines = request.BlindBoxLines ?? Array.Empty<BlindBoxOrderLine>();
        var frontendItems = request.Items ?? Array.Empty<FrontendOrderItemDto>();
        var useRequestedItems = frontendItems.Count > 0;

        if (cart.Items.Count == 0 && blindBoxLines.Count == 0 && frontendItems.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var subTotal = (useRequestedItems
                    ? await CalculateRequestedSubtotalAsync(frontendItems, cancellationToken)
                    : cart.SubTotal)
                + blindBoxLines.Sum(x => ResolveBlindBoxLinePrice(x.UnitPrice) * Math.Max(x.Quantity, 1));
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
            var isVnPay = IsVnPayPayment(request.PaymentMethod);
            VnPayPaymentUrlDto? vnPayPayment = null;
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                ReceiverName = request.ReceiverName ?? request.UserName ?? request.ShippingAddress?.Name ?? string.Empty,
                ReceiverPhone = request.ReceiverPhone ?? request.ShippingAddress?.Phone ?? string.Empty,
                ReceiverEmail = request.ReceiverEmail ?? request.UserEmail ?? string.Empty,
                ShippingAddress = BuildShippingAddressText(request),
                Notes = request.Notes ?? request.Note,
                Status = OrderStatus.Pending,
                PaymentMethod = ParsePaymentMethod(request.PaymentMethod),
                PaymentStatus = isVnPay ? "pending" : "unpaid",
                PaymentProvider = isVnPay ? "vnpay" : null,
                ShippingFee = shippingFee,
                Discount = discountAmount,
                TotalAmount = finalTotal
            };

            if (isVnPay)
            {
                vnPayPayment = _vnPayService.CreatePaymentUrl(
                    order.Id,
                    finalTotal,
                    $"Thanh toan don hang BookSoul {order.Id:N}",
                    clientIp ?? "127.0.0.1",
                    cancellationToken);
                order.PaymentTxnRef = vnPayPayment.TxnRef;
            }

            if (!useRequestedItems)
            {
                foreach (var item in cart.Items)
                {
                    await AddOrderLineFromCartAsync(order, item, cancellationToken);
                }
            }

            if (useRequestedItems)
            {
                foreach (var item in frontendItems)
                {
                    await AddOrderLineFromFrontendAsync(order, item, cancellationToken);
                }
            }

            foreach (var blind in blindBoxLines)
            {
                var pickedBook = await PickRandomBlindBoxBookAsync(null, cancellationToken);
                if (pickedBook is null)
                {
                    throw new InvalidOperationException("No eligible book available for blind box.");
                }

                if (pickedBook.Stock < blind.Quantity)
                {
                    throw new InvalidOperationException("Not enough stock for blind box assignment.");
                }

                pickedBook.Stock -= blind.Quantity;
                _context.Books.Update(pickedBook);
                order.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    BookId = pickedBook.Id,
                    AccessoryId = null,
                    BlindBoxTier = BlindBoxTier.Normal,
                    BlindBoxGenre = pickedBook.Category?.Name,
                    ProductName = "Blind Box",
                    ProductTypeText = "blindbox",
                    Category = pickedBook.Category?.Name,
                    Price = ResolveBlindBoxLinePrice(blind.UnitPrice),
                    Quantity = blind.Quantity
                });
            }

            _context.Orders.Add(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            try
            {
                if (!isVnPay && useRequestedItems)
                {
                    foreach (var item in frontendItems)
                    {
                        if (!Guid.TryParse(item.Id, out var productId)) continue;
                        var productType = (item.Type ?? "book").Trim().Equals("accessory", StringComparison.OrdinalIgnoreCase)
                            ? ProductType.Accessory
                            : ProductType.Book;
                        await _cartService.RemoveItemAsync(customerId, productId, productType, cancellationToken);
                    }
                }
                else if (!isVnPay)
                {
                    await _cartService.ClearCartAsync(customerId, cancellationToken);
                }
            }
            catch
            {
                // Checkout already succeeded; cart cleanup can be retried by the client.
            }

            try 
            {
                await _notificationService.BroadcastToRoleAsync("Admin", "Đơn hàng mới", $"Đơn hàng {order.Id} vừa được tạo", NotificationType.Order);
                await _notificationService.BroadcastToRoleAsync("Staff", "Đơn hàng mới", $"Đơn hàng {order.Id} vừa được tạo", NotificationType.Order);
            } 
            catch { }

            return new CheckoutResponseDto(
                order.Id,
                subTotal,
                shippingFee,
                discountAmount,
                finalTotal,
                ToFrontendStatus(order.Status),
                appliedCode,
                vnPayPayment?.PaymentUrl,
                order.PaymentStatus,
                order.PaymentProvider,
                order.PaymentTxnRef);
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
            _context.Books.Update(book);
            order.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                BookId = book.Id,
                AccessoryId = null,
                Quantity = item.Quantity,
                Price = book.Price,
                ProductName = book.Title,
                ProductImage = book.ImageUrl,
                ProductTypeText = "book",
                Author = book.AuthorName,
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
            _context.Accessories.Update(accessory);
            order.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                BookId = null,
                AccessoryId = accessory.Id,
                Quantity = item.Quantity,
                Price = accessory.Price,
                ProductName = accessory.Name,
                ProductImage = accessory.ImageUrl,
                ProductTypeText = "accessory",
                BlindBoxTier = null,
                BlindBoxGenre = null
            });
        }
    }

    private async Task AddOrderLineFromFrontendAsync(Order order, FrontendOrderItemDto item, CancellationToken cancellationToken)
    {
        var type = (item.Type ?? "book").Trim().ToLowerInvariant();
        var quantity = item.Quantity <= 0 ? 1 : item.Quantity;
        var name = item.Title ?? item.Name ?? "Sản phẩm";

        if (type == "blindbox")
        {
            var tier = ParseBlindBoxTier(item.Tier);
            var blindBoxPrice = GetBlindBoxPrice(tier);
            var selectedBook = await PickRandomBlindBoxBookAsync(item.Category, cancellationToken);
            if (selectedBook is null)
            {
                throw new InvalidOperationException("No eligible book available for blind box.");
            }

            if (selectedBook.Stock < quantity)
            {
                throw new InvalidOperationException("Not enough stock for blind box assignment.");
            }

            selectedBook.Stock -= quantity;
            _context.Books.Update(selectedBook);

            order.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                BookId = selectedBook.Id,
                AccessoryId = null,
                BlindBoxTier = tier,
                BlindBoxGenre = item.Category ?? selectedBook.Category?.Name,
                ProductName = name,
                ProductImage = item.Image,
                ProductTypeText = "blindbox",
                Category = item.Category ?? selectedBook.Category?.Name,
                Price = blindBoxPrice,
                Quantity = quantity
            });
            return;
        }

        if (Guid.TryParse(item.Id, out var productId))
        {
            if (type == "accessory")
            {
                var accessory = await _context.Accessories.FirstOrDefaultAsync(a => a.Id == productId, cancellationToken);
                if (accessory is not null)
                {
                    if (accessory.Stock < quantity) throw new InvalidOperationException($"Not enough stock for '{accessory.Name}'.");
                    accessory.Stock -= quantity;
                    _context.Accessories.Update(accessory);
                    order.OrderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        AccessoryId = accessory.Id,
                        ProductName = accessory.Name,
                        ProductImage = accessory.ImageUrl,
                        ProductTypeText = "accessory",
                        Brand = item.Brand,
                        Category = item.Category,
                        Price = accessory.Price,
                        Quantity = quantity
                    });
                    return;
                }
            }

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == productId, cancellationToken);
            if (book is not null)
            {
                if (book.Stock < quantity) throw new InvalidOperationException($"Not enough stock for '{book.Title}'.");
                book.Stock -= quantity;
                _context.Books.Update(book);
                order.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    BookId = book.Id,
                    ProductName = book.Title,
                    ProductImage = book.ImageUrl,
                    ProductTypeText = "book",
                    Author = book.AuthorName,
                    Category = item.Category,
                    Price = book.Price,
                    Quantity = quantity
                });
                return;
            }
        }

        order.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductName = name,
            ProductImage = item.Image,
            ProductTypeText = type,
            Author = item.Author,
            Brand = item.Brand,
            Category = item.Category,
            Price = item.Price,
            Quantity = quantity
        });
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

        if (status == OrderStatus.Cancelled && !CanCustomerCancel(order.Status))
        {
            throw new InvalidOperationException("Only orders before waiting for delivery can be cancelled.");
        }

        order.Status = status;
        if (status == OrderStatus.Cancelled)
        {
            await RestoreOrderStockAsync(order, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task<OrderSummaryDto> CancelOrderAsync(Guid customerId, Guid orderId, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        if (!CanCustomerCancel(order.Status))
        {
            throw new InvalidOperationException("Only orders before waiting for delivery can be cancelled.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Cancellation reason is required.");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = request.Reason.Trim();
        order.CancelledAt = DateTime.UtcNow;
        await RestoreOrderStockAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task<OrderSummaryDto> RequestReturnAsync(Guid customerId, Guid orderId, RequestReturnOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.ReturnRejected)
        {
            throw new InvalidOperationException("Only delivered orders can be returned.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Return reason is required.");
        }

        order.Status = OrderStatus.ReturnRequested;
        order.ReturnReason = request.Reason.Trim();
        order.ReturnReasonDetail = string.IsNullOrWhiteSpace(request.Detail) ? null : request.Detail.Trim();
        order.ReturnReviewNote = null;
        order.ReturnRequestedAt = DateTime.UtcNow;
        order.ReturnReviewedAt = null;

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
                cartItems.Add(new CartItemDto(
                    book.Id, ProductType.Book, book.Title, item.Quantity, book.Price, book.Price * item.Quantity, false,
                    book.Id.ToString(), "book", book.Title, book.Price, book.ImageUrl, book.AuthorName, null, null, null));
            }
            else if (item.AccessoryId.HasValue)
            {
                var accessory = await _context.Accessories.AsNoTracking().FirstOrDefaultAsync(a => a.Id == item.AccessoryId.Value, cancellationToken);
                if (accessory is null) continue;
                cartItems.Add(new CartItemDto(
                    accessory.Id, ProductType.Accessory, accessory.Name, item.Quantity, accessory.Price, accessory.Price * item.Quantity, false,
                    accessory.Id.ToString(), "accessory", accessory.Name, accessory.Price, accessory.ImageUrl, null, null, null, null));
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

    private async Task<Book?> PickRandomBlindBoxBookAsync(string? preferredCategory, CancellationToken cancellationToken)
    {
        var books = await _context.Books
            .Include(b => b.Category)
            .Where(b => b.IsActive && b.Stock > 0)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(preferredCategory))
        {
            var normalized = preferredCategory.Trim();
            var matchedBooks = books
                .Where(book => !string.IsNullOrWhiteSpace(book.Category?.Name)
                    && (book.Category.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                        || normalized.Contains(book.Category.Name, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(_ => Guid.NewGuid())
                .ToList();

            if (matchedBooks.Count > 0)
            {
                return matchedBooks.First();
            }
        }

        return books.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
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
        ToFrontendStatus(order.Status),
        ToFrontendPaymentMethod(order),
        order.OrderItems.Select(oi => new OrderItemDto(
            oi.BookId ?? oi.AccessoryId,
            oi.BookId.HasValue ? ProductType.Book : ProductType.Accessory,
            oi.ProductName ?? oi.BlindBoxGenre ?? (oi.BookId.HasValue ? "Book" : "Accessory"),
            oi.Quantity,
            oi.Price,
            oi.BlindBoxTier.HasValue,
            (oi.BookId ?? oi.AccessoryId)?.ToString(),
            oi.ProductTypeText ?? (oi.BlindBoxTier.HasValue ? "blindbox" : oi.BookId.HasValue ? "book" : "accessory"),
            oi.ProductName ?? oi.BlindBoxGenre ?? (oi.BookId.HasValue ? "Book" : "Accessory"),
            oi.Price,
            oi.ProductImage,
            oi.Author,
            oi.Brand,
            oi.Category ?? oi.BlindBoxGenre,
            oi.BlindBoxTier?.ToString())).ToList(),
        order.CustomerId.ToString(),
        order.ReceiverName,
        order.ReceiverEmail,
        order.OrderDate.ToString("O"),
        order.TotalAmount,
        new ShippingAddressDto(order.ReceiverName, order.ReceiverPhone, order.ShippingAddress, string.Empty),
        order.CancellationReason,
        order.CancelledAt,
        order.ReturnReason,
        order.ReturnReasonDetail,
        order.ReturnReviewNote,
        order.ReturnRequestedAt,
        order.ReturnReviewedAt,
        order.PaymentStatus,
        order.PaymentProvider,
        order.PaymentTxnRef,
        order.PaymentTransactionNo,
        order.PaymentResponseCode,
        order.PaidAt);

    private static string BuildShippingAddressText(CreateOrderRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ShippingAddressText)) return request.ShippingAddressText;
        if (request.ShippingAddress is null) return string.Empty;
        var parts = new[] { request.ShippingAddress.Address, request.ShippingAddress.City }
            .Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(", ", parts);
    }

    private static PaymentMethod ParsePaymentMethod(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod)) return PaymentMethod.CashOnDelivery;
        var normalized = paymentMethod.Trim().ToLowerInvariant();
        if (normalized.Contains("vnpay") || normalized.Contains("vn pay"))
            return PaymentMethod.EWallet;
        if (normalized.Contains("transfer") || normalized.Contains("khoản") || normalized.Contains("khoan") || normalized.Contains("bank"))
            return PaymentMethod.BankTransfer;
        if (normalized.Contains("wallet") || normalized.Contains("ví") || normalized.Contains("vi"))
            return PaymentMethod.EWallet;
        return PaymentMethod.CashOnDelivery;
    }

    private static bool IsVnPayPayment(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod)) return false;
        var normalized = paymentMethod.Trim().ToLowerInvariant();
        return normalized.Contains("vnpay") || normalized.Contains("vn pay");
    }

    private async Task<decimal> CalculateRequestedSubtotalAsync(
        IReadOnlyCollection<FrontendOrderItemDto> items,
        CancellationToken cancellationToken)
    {
        decimal total = 0;

        foreach (var item in items)
        {
            var quantity = item.Quantity <= 0 ? 1 : item.Quantity;
            var type = (item.Type ?? "book").Trim().ToLowerInvariant();

            if (type == "blindbox")
            {
                total += GetBlindBoxPrice(ParseBlindBoxTier(item.Tier)) * quantity;
                continue;
            }

            if (Guid.TryParse(item.Id, out var productId))
            {
                if (type == "accessory")
                {
                    var accessoryPrice = await _context.Accessories
                        .Where(accessory => accessory.Id == productId)
                        .Select(accessory => (decimal?)accessory.Price)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (accessoryPrice.HasValue)
                    {
                        total += accessoryPrice.Value * quantity;
                        continue;
                    }
                }

                var bookPrice = await _context.Books
                    .Where(book => book.Id == productId)
                    .Select(book => (decimal?)book.Price)
                    .FirstOrDefaultAsync(cancellationToken);

                if (bookPrice.HasValue)
                {
                    total += bookPrice.Value * quantity;
                    continue;
                }
            }

            total += item.Price * quantity;
        }

        return total;
    }

    public async Task RestoreOrderStockAsync(Order order, CancellationToken cancellationToken = default)
    {
        foreach (var item in order.OrderItems)
        {
            if (item.BookId.HasValue)
            {
                var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == item.BookId.Value, cancellationToken);
                if (book is not null)
                {
                    book.Stock += item.Quantity;
                    _context.Books.Update(book);
                }
            }
            else if (item.AccessoryId.HasValue)
            {
                var accessory = await _context.Accessories.FirstOrDefaultAsync(a => a.Id == item.AccessoryId.Value, cancellationToken);
                if (accessory is not null)
                {
                    accessory.Stock += item.Quantity;
                    _context.Accessories.Update(accessory);
                }
            }
        }
    }

    private static BlindBoxTier? ParseBlindBoxTier(string? tier)
    {
        if (string.IsNullOrWhiteSpace(tier)) return BlindBoxTier.Normal;
        var normalized = tier.Trim().ToLowerInvariant();
        if (normalized.Contains("deluxe")) return BlindBoxTier.Deluxe;
        if (normalized.Contains("pro")) return BlindBoxTier.Pro;
        return BlindBoxTier.Normal;
    }

    private static decimal GetBlindBoxPrice(BlindBoxTier? tier) => tier switch
    {
        BlindBoxTier.Deluxe => 249000m,
        BlindBoxTier.Pro => 149000m,
        _ => 99000m
    };

    private static decimal ResolveBlindBoxLinePrice(decimal unitPrice)
    {
        if (unitPrice >= 249000m) return GetBlindBoxPrice(BlindBoxTier.Deluxe);
        if (unitPrice >= 149000m) return GetBlindBoxPrice(BlindBoxTier.Pro);
        return GetBlindBoxPrice(BlindBoxTier.Normal);
    }

    private static bool CanCustomerCancel(OrderStatus status) => (int)status is 0 or 1;

    private static string ToFrontendStatus(OrderStatus status) => (int)status switch
    {
        0 => "pending",
        1 => "awaitingPreparation",
        2 => "readyForDelivery",
        3 => "readyForDelivery",
        4 => "delivered",
        5 => "cancelled",
        6 => "returnRequested",
        7 => "returned",
        8 => "returnRejected",
        _ => status.ToString().ToLowerInvariant()
    };

    private static string ToFrontendPaymentMethod(Order order)
    {
        if (order.PaymentProvider?.Equals("vnpay", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "VNPay";
        }

        return ToFrontendPaymentMethod(order.PaymentMethod);
    }

    private static string ToFrontendPaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.CashOnDelivery => "Thanh toán khi nhận hàng",
        PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
        PaymentMethod.EWallet => "Ví điện tử",
        _ => method.ToString()
    };
}
