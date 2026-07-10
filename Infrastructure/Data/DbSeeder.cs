using Domain.Entities.Books;
using Domain.Entities.Identity;
using Domain.Entities.System;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var roleName in new[] { "Customer", "Admin", "Staff", "Shipper" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role { Name = roleName });
            }
        }

        await SeedUserIfConfiguredAsync(userManager, configuration, "Seed:AdminEmail", "Seed:AdminPassword", "Admin", "System Administrator", cancellationToken);
        await SeedUserIfConfiguredAsync(userManager, configuration, "Seed:StaffEmail", "Seed:StaffPassword", "Staff", "Staff User", cancellationToken);
        await SeedUserIfConfiguredAsync(userManager, configuration, "Seed:ShipperEmail", "Seed:ShipperPassword", "Shipper", "Shipper User", cancellationToken);

        var shippingFee = configuration.GetValue<decimal?>("Seed:DefaultShippingFee") ?? 0m;
        if (!await context.SystemSettings.AnyAsync(cancellationToken))
        {
            context.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                ShippingFee = shippingFee
            });
            await context.SaveChangesAsync(cancellationToken);
        }

        await SeedBookCatalogAsync(context, cancellationToken);

        if (configuration.GetValue("Seed:SeedSampleVoucher", false))
        {
            const string code = "WELCOME10";
            if (!await context.Vouchers.AnyAsync(v => v.Code == code, cancellationToken))
            {
                context.Vouchers.Add(new Voucher
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    DiscountAmount = 10,
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    MinOrderValue = 100,
                    IsActive = true
                });
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static async Task SeedBookCatalogAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.Categories.AnyAsync(cancellationToken))
        {
            var categories = new[]
            {
                new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Tiểu thuyết", Description = "Sách văn học, truyện dài" },
                new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Tâm lý học", Description = "Sách phát triển bản thân, tâm lý" },
                new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Lịch sử", Description = "Sách lịch sử, kiến thức" },
                new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Phát triển bản thân", Description = "Sách kỹ năng, self-help" }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync(cancellationToken);
        }

        /*
        if (await context.Books.AnyAsync(cancellationToken)) return;

        var categoryMap = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id, cancellationToken);
        var books = new[]
        {
            new Book { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Code = "001", CreatedAt = DateTime.UtcNow.AddDays(-6), Title = "Nhà Giả Kim", AuthorName = "Paulo Coelho", CategoryId = categoryMap["Tiểu thuyết"], Price = 89000, Condition = BookCondition.Good, Stock = 12, ImageUrl = "https://baocantho.com.vn/image/news/2017/20170107/fckimage/40361498129094_102.jpg", Description = "Hành trình tìm kiếm ước mơ và ý nghĩa cuộc sống.", IsActive = true },
            new Book { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Code = "002", CreatedAt = DateTime.UtcNow.AddDays(-5), Title = "Đắc Nhân Tâm", AuthorName = "Dale Carnegie", CategoryId = categoryMap["Tâm lý học"], Price = 85000, Condition = BookCondition.Good, Stock = 8, ImageUrl = "https://images.unsplash.com/photo-1762968280286-0bfcc4afd0ea?auto=format&fit=crop&w=1080&q=80", Description = "Kỹ năng giao tiếp và ứng xử kinh điển.", IsActive = true },
            new Book { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Code = "003", CreatedAt = DateTime.UtcNow.AddDays(-4), Title = "Tôi Thấy Hoa Vàng Trên Cỏ Xanh", AuthorName = "Nguyễn Nhật Ánh", CategoryId = categoryMap["Tiểu thuyết"], Price = 79000, Condition = BookCondition.Good, Stock = 15, ImageUrl = "https://images.unsplash.com/photo-1754726876108-8875c3d210d9?auto=format&fit=crop&w=1080&q=80", Description = "Tuổi thơ, ký ức và những cảm xúc trong trẻo.", IsActive = true },
            new Book { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Code = "004", CreatedAt = DateTime.UtcNow.AddDays(-3), Title = "Sapiens: Lược Sử Loài Người", AuthorName = "Yuval Noah Harari", CategoryId = categoryMap["Lịch sử"], Price = 129000, Condition = BookCondition.Good, Stock = 6, ImageUrl = "https://images.unsplash.com/photo-1752243755828-70fe436946cb?auto=format&fit=crop&w=1080&q=80", Description = "Lịch sử loài người từ quá khứ đến hiện đại.", IsActive = true },
            new Book { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), Code = "005", CreatedAt = DateTime.UtcNow.AddDays(-2), Title = "7 Thói Quen Của Người Thành Đạt", AuthorName = "Stephen Covey", CategoryId = categoryMap["Phát triển bản thân"], Price = 108000, Condition = BookCondition.Good, Stock = 10, ImageUrl = "https://images.unsplash.com/photo-1605444610001-15c877be632a?auto=format&fit=crop&w=1080&q=80", Description = "7 nguyên tắc cốt lõi để thành công.", IsActive = true },
            new Book { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Code = "006", CreatedAt = DateTime.UtcNow.AddDays(-1), Title = "Khéo Ăn Nói Sẽ Có Được Thiên Hạ", AuthorName = "Trác Nhã", CategoryId = categoryMap["Phát triển bản thân"], Price = 72000, Condition = BookCondition.LikeNew, Stock = 9, ImageUrl = "https://images.unsplash.com/photo-1630838219194-88b85db13dce?auto=format&fit=crop&w=1080&q=80", Description = "Nghệ thuật giao tiếp khéo léo và thực tế.", IsActive = true }
        };

        context.Books.AddRange(books);
        await context.SaveChangesAsync(cancellationToken);
        */
    }

    private static async Task SeedUserIfConfiguredAsync(
        UserManager<User> userManager,
        IConfiguration configuration,
        string emailKey,
        string passwordKey,
        string role,
        string fullName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = configuration[emailKey];
        var password = configuration[passwordKey];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        email = email.Trim();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
            {
                await userManager.AddToRoleAsync(existing, role);
            }
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FullName = fullName,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Seed user failed ({email}): " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, role);
    }

}
