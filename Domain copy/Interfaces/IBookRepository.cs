using Domain.Entities.Books;
using Domain.Enums;

namespace Domain.Interfaces;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> SearchAsync(
        string? keyword,
        Guid? authorId,
        Guid? categoryId,
        BookCondition? condition,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default);

    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
