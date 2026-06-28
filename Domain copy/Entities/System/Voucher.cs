namespace Domain.Entities.System;

public class Voucher
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal MinOrderValue { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Orders.Order> Orders { get; set; } = new List<Orders.Order>();
}
