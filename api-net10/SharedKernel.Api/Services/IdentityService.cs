using Microsoft.AspNetCore.Http;
using SharedKernel.Application.DTO;
using SharedKernel.Application.Interfaces;

namespace SharedKernel.Api.Services;

public class IdentityService : IIdentityService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public HttpContext? httpContext => _httpContextAccessor.HttpContext;

    public CurrentUserDto? currentUser
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var idClaim = user.FindFirst("id")?.Value;
            return new CurrentUserDto
            {
                id = Guid.TryParse(idClaim, out var guid) ? guid : null,
                tai_khoan = user.FindFirst("tai_khoan")?.Value ?? string.Empty,
                ten = user.FindFirst("ten")?.Value,
                email = user.FindFirst("email")?.Value,
                is_super_admin = bool.TryParse(user.FindFirst("is_super_admin")?.Value, out var isAdmin) && isAdmin,
                don_vi_id = user.FindFirst("don_vi_id")?.Value
            };
        }
    }
}
