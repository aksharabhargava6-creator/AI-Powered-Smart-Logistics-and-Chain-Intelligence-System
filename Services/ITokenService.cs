using LogisticsPlatform.API.Models;

namespace LogisticsPlatform.API.Services;

public interface ITokenService
{
    (string token, DateTime expiresAtUtc) GenerateToken(ApplicationUser user, IList<string> roles);
}
