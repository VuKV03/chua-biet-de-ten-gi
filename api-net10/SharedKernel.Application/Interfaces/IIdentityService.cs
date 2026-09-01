using Microsoft.AspNetCore.Http;
using SharedKernel.Application.DTO;

namespace SharedKernel.Application.Interfaces;

public interface IIdentityService
{
    CurrentUserDto? currentUser { get; }
    HttpContext? httpContext { get; }
}
