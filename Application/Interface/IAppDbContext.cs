using Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interface;

public interface IAppDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<Domain.Entities.Notification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
