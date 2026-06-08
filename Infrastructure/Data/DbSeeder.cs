using Domain.Entities.Identity;
using Domain.Entities.System;
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

        foreach (var roleName in new[] { "Customer", "Admin", "Staff" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role { Name = roleName });
            }
        }

        await SeedUserIfConfiguredAsync(userManager, configuration, "Seed:AdminEmail", "Seed:AdminPassword", "Admin", "System Administrator", cancellationToken);
        await SeedUserIfConfiguredAsync(userManager, configuration, "Seed:StaffEmail", "Seed:StaffPassword", "Staff", "Staff User", cancellationToken);

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
