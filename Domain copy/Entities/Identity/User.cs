using Domain.Entities.Buyback;
using Domain.Entities.Import;
using Domain.Entities.Orders;
using Domain.Entities.Community;
using Domain.Entities.Reviews;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity;

public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();
    public ICollection<BuybackRequest> BuybackRequests { get; set; } = new List<BuybackRequest>();
    public ICollection<ImportTicket> ImportTickets { get; set; } = new List<ImportTicket>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<UserReadLog> ReadLogs { get; set; } = new List<UserReadLog>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
