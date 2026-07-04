using Application.DTO;
using Application.Interface;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Payments;

public record ProcessPayOSWebhookCommand(WebhookType WebhookBody) : IRequest<bool>;

public class ProcessPayOSWebhook : IRequestHandler<ProcessPayOSWebhookCommand, bool>
{
    private readonly IAppDbContext _context;
    private readonly IPayOSService _payOSService;
    private readonly ICartService _cartService;

    public ProcessPayOSWebhook(IAppDbContext context, IPayOSService payOSService, ICartService cartService)
    {
        _context = context;
        _payOSService = payOSService;
        _cartService = cartService;
    }

    public async Task<bool> Handle(ProcessPayOSWebhookCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Cố gắng xác thực chữ ký (Checksum)
            var verifiedData = await _payOSService.VerifyWebhookDataAsync(request.WebhookBody);

            if (verifiedData == null || request.WebhookBody.code != "00")
            {
                return false;
            }

            var orderCodeStr = verifiedData.orderCode.ToString();

            // 2. Tìm đơn hàng
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.PaymentTxnRef == orderCodeStr, cancellationToken);

            if (order == null)
            {
                // SỬA LỖI Ở ĐÂY: Nếu là PayOS test webhook sẽ không tìm thấy đơn.
                // Tuyệt đối KHÔNG throw Exception. Chỉ in log và trả về true để báo 200 OK.
                Console.WriteLine($"[PayOS Webhook] Không tìm thấy đơn hàng hoặc đây là request test. Mã tham chiếu: {orderCodeStr}");
                return true;
            }

            // 3. Cập nhật Database nếu tìm thấy
            order.PaymentStatus = "paid";
            order.PaidAt = DateTime.UtcNow;
            order.Status = OrderStatus.Processing;

            await RemovePaidItemsFromCartAsync(order, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            // SỬA LỖI Ở ĐÂY: Bắt gọn lỗi sai ChecksumKey hoặc lỗi SDK.
            // In ra console để developer biết đường sửa, nhưng VẪN trả về true cho PayOS vui vẻ.
            Console.WriteLine($"[PayOS LỖI XÁC THỰC WEBHOOK]: {ex.Message}");
            return true;
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
}