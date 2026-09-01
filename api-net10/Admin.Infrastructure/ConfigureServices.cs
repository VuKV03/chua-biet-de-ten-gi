using Admin.Application.Auth.Commands;
using Admin.Application.Interfaces;
using Admin.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using SharedKernel.Application.Behaviours;
using SharedKernel.Infrastructure;

namespace Admin.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddAdminInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

        services.AddSharedKernelInfrastructureServices();

        services.AddDbContext<AdminDbContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
            {
                mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore);
            });
        });

        services.AddScoped<IAdminDbContext>(provider => provider.GetRequiredService<AdminDbContext>());

        // Dùng type marker thực (LoginCommand) trong Admin.Application thay vì AppDomain.Load("Admin.Application")
        // theo tên chuỗi — an toàn hơn khi refactor/rename assembly và tránh lỗi runtime nếu assembly chưa được load.
        var appAssembly = typeof(LoginCommand).Assembly;

        // Đăng ký MediatR (MediatR v14 syntax) + Pipeline Behavior chạy FluentValidation trước Handler
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(appAssembly);
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        // Đăng ký toàn bộ AbstractValidator<T> trong Admin.Application (Login/RefreshToken/...)
        services.AddValidatorsFromAssembly(appAssembly);

        services.AddAutoMapper(cfg => { }, appAssembly);

        return services;
    }
}
