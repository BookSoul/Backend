using Domain.Entities.Identity;

namespace Application.Interface;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, IList<string> roles);
}
