using Domain.Entities.Identity;
using Domain.Entities.System;
using Domain.Enums;

namespace Domain.Entities.Orders;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ReceiverEmail { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string PaymentStatus { get; set; } = "unpaid";
    public string? PaymentProvider { get; set; }
    public string? PaymentTxnRef { get; set; }
    public string? PaymentTransactionNo { get; set; }
    public string? PaymentResponseCode { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? ReturnReason { get; set; }
    public string? ReturnReasonDetail { get; set; }
    public string? ReturnReviewNote { get; set; }
    public DateTime? ReturnRequestedAt { get; set; }
    public DateTime? ReturnReviewedAt { get; set; }

    public User Customer { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
