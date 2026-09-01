using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Application.Interfaces;
using SharedKernel.Infrastructure.Data;
using SharedKernel.Infrastructure.Services;

namespace SharedKernel.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddSharedKernelInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        return services;
    }
}
