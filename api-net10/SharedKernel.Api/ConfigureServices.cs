using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Api.Services;
using SharedKernel.Application.Interfaces;

namespace SharedKernel.Api;

public static class ConfigureServices
{
    public static IServiceCollection AddSharedKernelApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IIdentityService, IdentityService>();
        return services;
    }
}
