using Domain.Entities.Accessories;
using Domain.Entities.Books;
using Domain.Entities.Buyback;
using Domain.Entities.Community;
using Domain.Entities.Donate;
using Domain.Entities.Identity;
using Domain.Entities.Import;
using Domain.Entities.Orders;
using Domain.Entities.Reviews;
using Domain.Entities.System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, Role, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Accessory> Accessories => Set<Accessory>();
    public DbSet<AccessoryType> AccessoryTypes => Set<AccessoryType>();
    public DbSet<ImportTicket> ImportTickets => Set<ImportTicket>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<UserReadLog> UserReadLogs => Set<UserReadLog>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<DonateRequest> DonateRequests => Set<DonateRequest>();
    public DbSet<BuybackRequest> BuybackRequests => Set<BuybackRequest>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Order>().HasKey(o => o.Id);
        modelBuilder.Entity<OrderItem>().HasKey(oi => new { oi.OrderId, oi.BookId, oi.AccessoryId });
        modelBuilder.Entity<ShoppingCartItem>().HasIndex(ci => new { ci.CustomerId, ci.BookId, ci.AccessoryId }).IsUnique();
        modelBuilder.Entity<WishlistItem>().HasIndex(x => new { x.CustomerId, x.BookId, x.AccessoryId }).IsUnique();
        modelBuilder.Entity<BuybackRequest>().HasIndex(x => x.RequestCode).IsUnique();
        modelBuilder.Entity<DonateRequest>().Property(x => x.ImageUrls).HasMaxLength(4000);
        modelBuilder.Entity<BuybackRequest>().Property(x => x.ImageUrls).HasMaxLength(4000);
        modelBuilder.Entity<SystemSetting>().Property(s => s.ShippingFee).HasPrecision(18, 2);
        modelBuilder.Entity<Voucher>().Property(v => v.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Voucher>().Property(v => v.MinOrderValue).HasPrecision(18, 2);
        ConfigureDecimals(modelBuilder);
    }

    private static void ConfigureDecimals(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>().Property(b => b.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Accessory>().Property(a => a.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(o => o.ShippingFee).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(o => o.Discount).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(oi => oi.Price).HasPrecision(18, 2);
        modelBuilder.Entity<BuybackRequest>().Property(r => r.ProposedPrice).HasPrecision(18, 2);
        modelBuilder.Entity<BuybackRequest>().Property(r => r.ApprovedPrice).HasPrecision(18, 2);
    }
}
