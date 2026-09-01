using System.Security.Claims;
using SharedKernel.Application.DTO;

namespace SharedKernel.Application.Interfaces;

public interface IJwtTokenService
{
    (string token, string jwtId, DateTime expires) GenerateAccessToken(CurrentUserDto user);
    (string rawToken, string tokenHash, DateTime expires) GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
