using Domain.Entities.Books;
using Domain.Entities.Identity;
using Domain.Entities.Orders;
using Domain.Entities.Reviews;
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

        // ── REVIEW TEST DATA ──────────────────────────────────────────────
        if (configuration.GetValue("Seed:SeedReviewTestData", false))
        {
            await SeedReviewTestDataAsync(context, userManager, cancellationToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REVIEW TEST DATA SEEDER
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo dữ liệu mẫu để test chức năng review:
    /// - 3 tài khoản customer (email: customer1@test.com, customer2@test.com, customer3@test.com | pass: Test@1234)
    /// - 6 cuốn sách (sử dụng lại GUID cố định từ catalog seeder)
    /// - 3 đơn hàng trạng thái Delivered (mỗi customer 1 đơn, mỗi đơn 2 cuốn sách)
    /// - 3 review sẵn có (customer1 → sách 1, customer2 → sách 3 + sách 4)
    /// - customer1 CHƯA review sách 2 → có thể test CREATE
    /// - customer3 CHƯA review gì → có thể test CREATE đầy đủ
    /// - Thử review sách 1 lần 2 bằng customer1 → phải báo lỗi duplicate
    /// </summary>
    private static async Task SeedReviewTestDataAsync(AppDbContext context, UserManager<User> userManager, CancellationToken ct)
    {
        // ── Fixed GUIDs ────────────────────────────────────────────────────
        var bookIds = new[]
        {
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        };

        var customerEmails = new[] { "customer1@test.com", "customer2@test.com", "customer3@test.com" };
        const string testPassword = "Test@1234";

        // ── 1. Ensure books exist ──────────────────────────────────────────
        if (!await context.Books.AnyAsync(ct))
        {
            var catId = await context.Categories
                .Where(c => c.Name == "Tiểu thuyết")
                .Select(c => c.Id)
                .FirstOrDefaultAsync(ct);

            if (catId == Guid.Empty) return; // categories not seeded yet

            var fallback = await context.Categories.Select(c => c.Id).FirstOrDefaultAsync(ct);
            var books = new[]
            {
                new Book { Id = bookIds[0], Code = "TEST-001", CreatedAt = DateTime.UtcNow, Title = "Nhà Giả Kim",                   AuthorName = "Paulo Coelho",     CategoryId = catId,     Price = 89_000,  OriginalPrice = 110_000, Condition = BookCondition.Good,    Stock = 12, IsActive = true, Description = "Hành trình tìm kiếm ước mơ và ý nghĩa cuộc sống." },
                new Book { Id = bookIds[1], Code = "TEST-002", CreatedAt = DateTime.UtcNow, Title = "Đắc Nhân Tâm",                   AuthorName = "Dale Carnegie",    CategoryId = catId,     Price = 85_000,  OriginalPrice = 100_000, Condition = BookCondition.Good,    Stock = 8,  IsActive = true, Description = "Kỹ năng giao tiếp và ứng xử kinh điển." },
                new Book { Id = bookIds[2], Code = "TEST-003", CreatedAt = DateTime.UtcNow, Title = "Tôi Thấy Hoa Vàng Trên Cỏ Xanh", AuthorName = "Nguyễn Nhật Ánh", CategoryId = catId,     Price = 79_000,  OriginalPrice = 95_000,  Condition = BookCondition.Good,    Stock = 15, IsActive = true, Description = "Tuổi thơ, ký ức và những cảm xúc trong trẻo." },
                new Book { Id = bookIds[3], Code = "TEST-004", CreatedAt = DateTime.UtcNow, Title = "Sapiens: Lược Sử Loài Người",     AuthorName = "Yuval Harari",     CategoryId = fallback,  Price = 129_000, OriginalPrice = 150_000, Condition = BookCondition.Good,    Stock = 6,  IsActive = true, Description = "Lịch sử loài người từ quá khứ đến hiện đại." },
                new Book { Id = bookIds[4], Code = "TEST-005", CreatedAt = DateTime.UtcNow, Title = "7 Thói Quen Của Người Thành Đạt", AuthorName = "Stephen Covey",   CategoryId = fallback,  Price = 108_000, OriginalPrice = 130_000, Condition = BookCondition.LikeNew, Stock = 10, IsActive = true, Description = "7 nguyên tắc cốt lõi để thành công." },
                new Book { Id = bookIds[5], Code = "TEST-006", CreatedAt = DateTime.UtcNow, Title = "Khéo Ăn Nói Sẽ Có Được Thiên Hạ", AuthorName = "Trác Nhã",        CategoryId = fallback,  Price = 72_000,  OriginalPrice = 90_000,  Condition = BookCondition.LikeNew, Stock = 9,  IsActive = true, Description = "Nghệ thuật giao tiếp khéo léo và thực tế." },
            };
            context.Books.AddRange(books);
            await context.SaveChangesAsync(ct);
        }

        // ── 2. Create test customer accounts ──────────────────────────────
        var customerIds = new Guid[3];
        for (int i = 0; i < customerEmails.Length; i++)
        {
            var email = customerEmails[i];
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                customerIds[i] = existing.Id;
                continue;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                FullName = i == 0 ? "Nguyễn Văn Test" : i == 1 ? "Trần Thị Test" : "Lê Văn Test",
                AvatarUrl = null,
                EmailConfirmed = true,
                LockoutEnabled = true,
            };
            var result = await userManager.CreateAsync(user, testPassword);
            if (!result.Succeeded) throw new InvalidOperationException($"Seed customer failed ({email}): " + string.Join(", ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(user, "Customer");
            customerIds[i] = user.Id;
        }

        // ── 3. Create Delivered orders (1 per customer, 2 books each) ────
        //       Fixed order GUIDs to keep seed idempotent
        var orderGuids = new[]
        {
            Guid.Parse("00001111-0001-0001-0001-000000000001"),
            Guid.Parse("00001111-0002-0002-0002-000000000002"),
            Guid.Parse("00001111-0003-0003-0003-000000000003"),
        };

        var bookPairs = new[]
        {
            (bookIds[0], "Nhà Giả Kim",                    bookIds[1], "Đắc Nhân Tâm"),
            (bookIds[2], "Tôi Thấy Hoa Vàng Trên Cỏ Xanh", bookIds[3], "Sapiens: Lược Sử Loài Người"),
            (bookIds[4], "7 Thói Quen Của Người Thành Đạt", bookIds[5], "Khéo Ăn Nói Sẽ Có Được Thiên Hạ"),
        };

        for (int i = 0; i < 3; i++)
        {
            if (await context.Orders.AnyAsync(o => o.Id == orderGuids[i], ct)) continue;

            var order = new Order
            {
                Id = orderGuids[i],
                CustomerId = customerIds[i],
                OrderDate = DateTime.UtcNow.AddDays(-10 + i),
                ReceiverName = i == 0 ? "Nguyễn Văn Test" : i == 1 ? "Trần Thị Test" : "Lê Văn Test",
                ReceiverPhone = $"090000000{i + 1}",
                ReceiverEmail = customerEmails[i],
                ShippingAddress = $"{i + 1} Đường Test, Quận {i + 1}, TP.HCM",
                PaymentMethod = PaymentMethod.CashOnDelivery,
                ShippingFee = 30_000,
                Discount = 0,
                TotalAmount = 200_000 + i * 50_000m,
                Status = OrderStatus.Delivered,
                PaymentStatus = "paid",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        BookId = bookPairs[i].Item1,
                        ProductName = bookPairs[i].Item2,
                        ProductTypeText = "Sách",
                        Price = 89_000,
                        Quantity = 1,
                    },
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        BookId = bookPairs[i].Item3,
                        ProductName = bookPairs[i].Item4,
                        ProductTypeText = "Sách",
                        Price = 85_000,
                        Quantity = 1,
                    },
                }
            };
            context.Orders.Add(order);
        }
        await context.SaveChangesAsync(ct);

        // ── 4. Seed some existing reviews ─────────────────────────────────
        //   customer1 đã review sách 1 (Nhà Giả Kim)      → test GET + duplicate block
        //   customer2 đã review sách 3 + sách 4            → test GET list
        //   customer1 CHƯA review sách 2 (Đắc Nhân Tâm)   → test CREATE
        //   customer3 CHƯA review gì                       → test CREATE đầy đủ
        var reviewSeeds = new[]
        {
            new { Id = Guid.Parse("eeee0001-0001-0001-0001-000000000001"), CustomerId = customerIds[0], BookId = bookIds[0], Rating = 5, Comment = "Nhà Giả Kim thực sự là một tác phẩm tuyệt vời! Thay đổi cách nhìn của tôi về cuộc sống." },
            new { Id = Guid.Parse("eeee0002-0001-0001-0001-000000000001"), CustomerId = customerIds[1], BookId = bookIds[2], Rating = 4, Comment = "Câu chuyện cảm động và đầy ý nghĩa. Đọc xong mà vẫn còn cảm xúc mãi." },
            new { Id = Guid.Parse("eeee0002-0002-0002-0002-000000000002"), CustomerId = customerIds[1], BookId = bookIds[3], Rating = 5, Comment = "Sapiens mở ra nhiều góc nhìn mới về lịch sử nhân loại. Bắt buộc phải đọc!" },
        };

        foreach (var r in reviewSeeds)
        {
            if (await context.Reviews.AnyAsync(x => x.Id == r.Id, ct)) continue;
            context.Reviews.Add(new Review
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                BookId = r.BookId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
            });
        }
        await context.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BOOK CATALOG SEEDER
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedBookCatalogAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.Categories.AnyAsync(cancellationToken))
        {
            var categories = new[]
            {
                new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Tiểu thuyết",         Description = "Sách văn học, truyện dài" },
                new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Tâm lý học",           Description = "Sách phát triển bản thân, tâm lý" },
                new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Lịch sử",              Description = "Sách lịch sử, kiến thức" },
                new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Phát triển bản thân",  Description = "Sách kỹ năng, self-help" }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

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
