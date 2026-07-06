using System.Net;
using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IVnPayService _vnPayService;
    private readonly ICartService _cartService;
    private readonly IConfiguration _configuration;

    public PaymentsController(
        AppDbContext context,
        IVnPayService vnPayService,
        ICartService cartService,
        IConfiguration configuration)
    {
        _context = context;
        _vnPayService = vnPayService;
        _cartService = cartService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var result = _vnPayService.VerifyReturnData(ToDictionary(Request.Query), cancellationToken);
        var response = await ApplyVnPayResultAsync(result, cancellationToken);
        var frontendUrl = _configuration["VnPay:FrontendReturnUrl"] ?? "http://localhost:5173/payment/vnpay-return";
        var separator = frontendUrl.Contains('?') ? "&" : "?";
        var redirectUrl = string.Concat(
            frontendUrl,
            separator,
            "success=", response.Success ? "true" : "false",
            "&orderId=", WebUtility.UrlEncode(response.OrderId?.ToString() ?? string.Empty),
            "&paymentStatus=", WebUtility.UrlEncode(response.PaymentStatus),
            "&responseCode=", WebUtility.UrlEncode(response.ResponseCode ?? string.Empty),
            "&transactionNo=", WebUtility.UrlEncode(response.TransactionNo ?? string.Empty),
            "&message=", WebUtility.UrlEncode(response.Message));

        return Redirect(redirectUrl);
    }

    [AllowAnonymous]
    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var result = _vnPayService.VerifyReturnData(ToDictionary(Request.Query), cancellationToken);
        var response = await ApplyVnPayResultAsync(result, cancellationToken);

        if (!result.IsValidSignature)
        {
            return Ok(new { RspCode = "97", Message = "Invalid signature" });
        }

        if (response.OrderId is null)
        {
            return Ok(new { RspCode = "01", Message = "Order not found" });
        }

        if (!response.Success && response.Message.Contains("amount", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { RspCode = "04", Message = "Invalid amount" });
        }

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    private async Task<VnPayReturnResponseDto> ApplyVnPayResultAsync(VnPayVerificationResultDto result, CancellationToken cancellationToken)
    {
        var orderId = ParseOrderId(result.TxnRef);
        if (orderId is null)
        {
            return new VnPayReturnResponseDto(null, false, "Không tìm thấy mã đơn hàng trong phản hồi VNPay.", "failed", result.ResponseCode, result.TransactionNo);
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId.Value, cancellationToken);

        if (order is null)
        {
            return new VnPayReturnResponseDto(orderId, false, "Không tìm thấy đơn hàng.", "failed", result.ResponseCode, result.TransactionNo);
        }

        if (!result.IsValidSignature)
        {
            order.PaymentStatus = "invalid";
            order.PaymentResponseCode = result.ResponseCode;
            await _context.SaveChangesAsync(cancellationToken);
            return new VnPayReturnResponseDto(order.Id, false, "Chữ ký VNPay không hợp lệ.", order.PaymentStatus, result.ResponseCode, result.TransactionNo);
        }

        if (Math.Round(order.TotalAmount, 0, MidpointRounding.AwayFromZero) != Math.Round(result.Amount, 0, MidpointRounding.AwayFromZero))
        {
            await CancelFailedVnPayOrderAsync(order, "VNPay trả về sai số tiền thanh toán.", result, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return new VnPayReturnResponseDto(order.Id, false, "VNPay trả về sai số tiền thanh toán.", order.PaymentStatus, result.ResponseCode, result.TransactionNo);
        }

        order.PaymentProvider = "vnpay";
        order.PaymentTxnRef = result.TxnRef;
        order.PaymentTransactionNo = result.TransactionNo;
        order.PaymentResponseCode = result.ResponseCode;

        if (result.IsSuccess)
        {
            if (order.PaymentStatus == "paid")
            {
                return new VnPayReturnResponseDto(order.Id, true, "Thanh toÃ¡n VNPay thÃ nh cÃ´ng.", order.PaymentStatus, result.ResponseCode, result.TransactionNo);
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                order.PaymentStatus = "failed";
                await _context.SaveChangesAsync(cancellationToken);
                return new VnPayReturnResponseDto(order.Id, false, "Đơn hàng đã bị hủy trước khi VNPay xác nhận thanh toán.", order.PaymentStatus, result.ResponseCode, result.TransactionNo);
            }

            order.PaymentStatus = "paid";
            order.PaidAt ??= result.PayDate ?? DateTime.UtcNow;
            await RemovePaidItemsFromCartAsync(order, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return new VnPayReturnResponseDto(order.Id, true, "Thanh toán VNPay thành công.", order.PaymentStatus, result.ResponseCode, result.TransactionNo);
        }

        await CancelFailedVnPayOrderAsync(order, "Thanh toán VNPay không thành công hoặc đã bị hủy.", result, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return new VnPayReturnResponseDto(order.Id, false, "Thanh toán VNPay không thành công hoặc đã bị hủy.", order.PaymentStatus, result.ResponseCode, result.TransactionNo);
    }

    private async Task CancelFailedVnPayOrderAsync(
        Domain.Entities.Orders.Order order,
        string reason,
        VnPayVerificationResultDto result,
        CancellationToken cancellationToken)
    {
        if (order.PaymentStatus == "pending" && order.Status != OrderStatus.Cancelled)
        {
            await RestoreOrderStockAsync(order, cancellationToken);
        }

        order.Status = OrderStatus.Cancelled;
        order.CancellationReason ??= reason;
        order.CancelledAt ??= DateTime.UtcNow;
        order.PaymentStatus = "failed";
        order.PaymentResponseCode = result.ResponseCode;
        order.PaymentTransactionNo = result.TransactionNo;
    }

    private async Task RestoreOrderStockAsync(Domain.Entities.Orders.Order order, CancellationToken cancellationToken)
    {
        foreach (var item in order.OrderItems)
        {
            if (item.BookId.HasValue)
            {
                var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == item.BookId.Value, cancellationToken);
                if (book is not null)
                {
                    book.Stock += item.Quantity;
                }
            }
            else if (item.AccessoryId.HasValue)
            {
                var accessory = await _context.Accessories.FirstOrDefaultAsync(a => a.Id == item.AccessoryId.Value, cancellationToken);
                if (accessory is not null)
                {
                    accessory.Stock += item.Quantity;
                }
            }
        }
    }

    private async Task RemovePaidItemsFromCartAsync(Domain.Entities.Orders.Order order, CancellationToken cancellationToken)
    {
        foreach (var item in order.OrderItems)
        {
            if (item.BookId.HasValue)
            {
                await _cartService.RemoveItemAsync(order.CustomerId, item.BookId.Value, ProductType.Book, cancellationToken);
            }
            else if (item.AccessoryId.HasValue)
            {
                await _cartService.RemoveItemAsync(order.CustomerId, item.AccessoryId.Value, ProductType.Accessory, cancellationToken);
            }
        }
    }

    private static IReadOnlyDictionary<string, string> ToDictionary(IQueryCollection query)
    {
        return query.ToDictionary(pair => pair.Key, pair => pair.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    private static Guid? ParseOrderId(string txnRef)
    {
        if (Guid.TryParseExact(txnRef, "N", out var compactId)) return compactId;
        if (Guid.TryParse(txnRef, out var id)) return id;
        return null;
    }
}
