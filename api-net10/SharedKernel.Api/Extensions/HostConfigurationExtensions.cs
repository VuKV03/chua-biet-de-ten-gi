using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace SharedKernel.Api.Extensions;

public static class HostConfigurationExtensions
{
    /// <summary>
    /// Cấu hình xác thực JWT Bearer chuẩn dùng chung cho MỌI service.
    /// Service nào cũng đọc cùng JwtSettings:Secret/Issuer/Audience nên token do
    /// Admin.Api cấp dùng được ở tất cả service khác mà không cần gọi chéo.
    /// </summary>
    public static IServiceCollection AddSharedKernelJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("Missing 'JwtSettings:Secret' configuration.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>Swagger + nút Authorize Bearer, dùng chung cho mọi service.</summary>
    public static IServiceCollection AddSharedKernelSwagger(this IServiceCollection services, string title)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = title, Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Dán riêng accessToken vào đây (KHÔNG kèm chữ 'Bearer ' phía trước).",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });
            // Microsoft.OpenApi 2.x: dùng OpenApiSecuritySchemeReference, không còn .Reference
            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });
        return services;
    }
}