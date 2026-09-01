using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using SharedKernel.Application.Behaviours;
using SharedKernel.Infrastructure;
using Todo.Application.Commands;
using Todo.Application.Interfaces;
using Todo.Infrastructure.Data;

namespace Todo.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddTodoInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

        // Đăng ký AuditableEntitySaveChangesInterceptor (+ hasher/jwt service dùng chung)
        services.AddSharedKernelInfrastructureServices();

        services.AddDbContext<TodoDbContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
            {
                mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore);
            });
        });

        services.AddScoped<ITodoDbContext>(provider => provider.GetRequiredService<TodoDbContext>());

        // Quét assembly Todo.Application: MediatR handler + validator + AutoMapper profile
        var appAssembly = typeof(CreateTodoCommand).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(appAssembly);
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(appAssembly);
        services.AddAutoMapper(cfg => { }, appAssembly);

        return services;
    }
}