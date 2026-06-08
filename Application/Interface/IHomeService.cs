using Application.DTO;

namespace Application.Interface;

public interface IHomeService
{
    Task<HomePageDto> GetHomePageAsync(CancellationToken cancellationToken = default);
}
