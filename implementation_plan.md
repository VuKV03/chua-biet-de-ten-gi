# Kế hoạch Xây dựng Dự án Mới — Auth Flow Chuẩn Doanh nghiệp (.NET 10)

> **Bối cảnh**: Xây dựng dự án **hoàn toàn mới từ số 0** trên nền tảng **.NET 10**, kế thừa kiến trúc Clean Architecture + CQRS của dự án tham chiếu `RJS_api_net6`. 
> **Tiêu chí**: Ưu tiên **tính ổn định cao**, cú pháp hiện đại, loại bỏ các thư viện cũ/bị deprecated (ví dụ: `MediatR` v12+ đã tích hợp sẵn DI, không cần package mở rộng riêng).

---

## Tổng quan 9 Phase

```mermaid
graph LR
    P0["Phase 0<br/>Tạo Solution .NET 10<br/>& cài Package ổn định"] --> P1["Phase 1<br/>Tạo Database<br/>MySQL"]
    P1 --> P2["Phase 2<br/>SharedKernel<br/>.Domain"]
    P2 --> P3["Phase 3<br/>SharedKernel<br/>.Application"]
    P3 --> P4["Phase 4<br/>SharedKernel<br/>.Infrastructure"]
    P4 --> P5["Phase 5<br/>SharedKernel<br/>.Api"]
    P5 --> P6["Phase 6<br/>Admin.Domain<br/>& Application"]
    P6 --> P7["Phase 7<br/>Admin<br/>.Infrastructure"]
    P7 --> P8["Phase 8<br/>Admin.Api<br/>Build & Run"]
```

---

## Phase 0 — Tạo Solution & Project bằng CLI (.NET 10)

### 0.1. Tạo thư mục gốc & Solution
```powershell
mkdir MyAuthProject
cd MyAuthProject
dotnet new sln -n MyAuthProject
```

### 0.2. Tạo 8 Project (.NET 10)
```powershell
# SharedKernel layer (hạ tầng dùng chung)
dotnet new classlib -n SharedKernel.Domain -f net10.0
dotnet new classlib -n SharedKernel.Application -f net10.0
dotnet new classlib -n SharedKernel.Infrastructure -f net10.0
dotnet new classlib -n SharedKernel.Api -f net10.0

# Admin module (module nghiệp vụ & Auth)
dotnet new classlib -n Admin.Domain -f net10.0
dotnet new classlib -n Admin.Application -f net10.0
dotnet new classlib -n Admin.Infrastructure -f net10.0
dotnet new webapi   -n Admin.Api -f net10.0
```

### 0.3. Thêm các Project vào Solution
```powershell
dotnet sln add SharedKernel.Domain SharedKernel.Application SharedKernel.Infrastructure SharedKernel.Api
dotnet sln add Admin.Domain Admin.Application Admin.Infrastructure Admin.Api
```

### 0.4. Thiết lập Project Reference (Thứ tự phụ thuộc)
```powershell
# SharedKernel
dotnet add SharedKernel.Application reference SharedKernel.Domain
dotnet add SharedKernel.Infrastructure reference SharedKernel.Application
dotnet add SharedKernel.Api reference SharedKernel.Application SharedKernel.Infrastructure

# Admin Module
dotnet add Admin.Domain reference SharedKernel.Domain
dotnet add Admin.Application reference Admin.Domain SharedKernel.Application
dotnet add Admin.Infrastructure reference Admin.Application SharedKernel.Infrastructure
dotnet add Admin.Api reference Admin.Infrastructure SharedKernel.Api
```

### 0.5. Cài đặt Thư viện NuGet (.NET 10 Chuẩn & Ổn định)

> [!NOTE]
> - Trong .NET hiện đại, **MediatR (v12+)** đã tích hợp sẵn Dependency Injection, không cần cài `MediatR.Extensions.Microsoft.DependencyInjection` riêng.
> - Bỏ các thư viện cũ không còn cần thiết, chỉ giữ lại các package chuẩn công nghiệp ổn định nhất.

```powershell
# 1. SharedKernel.Domain
dotnet add SharedKernel.Domain package MediatR

# 2. SharedKernel.Application
dotnet add SharedKernel.Application package MediatR
dotnet add SharedKernel.Application package AutoMapper
dotnet add SharedKernel.Application package FluentValidation.DependencyInjectionExtensions
dotnet add SharedKernel.Application package Microsoft.EntityFrameworkCore

# 3. SharedKernel.Infrastructure
dotnet add SharedKernel.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add SharedKernel.Infrastructure package Pomelo.EntityFrameworkCore.MySql
dotnet add SharedKernel.Infrastructure package Microsoft.AspNetCore.Cryptography.KeyDerivation

# 4. SharedKernel.Api
dotnet add SharedKernel.Api package Microsoft.AspNetCore.Authentication.JwtBearer

# 5. Admin.Infrastructure
dotnet add Admin.Infrastructure package Pomelo.EntityFrameworkCore.MySql
dotnet add Admin.Infrastructure package Microsoft.EntityFrameworkCore.Design

# 6. Admin.Api
dotnet add Admin.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add Admin.Api package Swashbuckle.AspNetCore
```

### 0.6. Dọn file mặc định
Xóa các file `Class1.cs` sinh tự động trong mỗi classlib và các file `WeatherForecast*` trong `Admin.Api`.

---

## Phase 1 — Tạo Database MySQL

Chạy script SQL sau trên MySQL (MySQL 8.0+ / MariaDB):

```sql
CREATE DATABASE IF NOT EXISTS `my_auth_db`
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `my_auth_db`;

-- ═══════════════════════════════════════
-- Bảng 1: nguoi_dung
-- ═══════════════════════════════════════
CREATE TABLE `nguoi_dung` (
    `id`                     char(36)     NOT NULL,
    `tai_khoan`              varchar(255) NOT NULL,
    `mat_khau`               varchar(512) NOT NULL,
    `salt_code`              varchar(255) NULL DEFAULT '',
    `ten`                    varchar(255) NOT NULL,
    `email`                  varchar(255) NULL,
    `so_dien_thoai`          varchar(32)  NULL,
    `gioi_tinh`              tinyint(1)   NULL,
    `ngay_sinh`              datetime     NULL,
    `don_vi_id`              char(36)     NULL,
    `chuc_vu`                varchar(255) NULL,
    `trang_thai`             tinyint(1)   NOT NULL DEFAULT 1, -- 1: Hoạt động, 0: Khóa
    `is_super_admin`         tinyint(1)   NOT NULL DEFAULT 0,
    `is_doi_mk`              tinyint(1)   NULL DEFAULT 0,
    `ngay_doi_mk_gan_nhat`   datetime     NULL,
    `so_lan_dang_nhap_sai`   int          NOT NULL DEFAULT 0,
    `khoa_den_ngay`          datetime     NULL,
    `password_hash_type`     int          NOT NULL DEFAULT 1, -- 1: PBKDF2/SHA512
    `ngay_tao`               datetime     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `nguoi_tao`              varchar(255) NULL,
    `ngay_chinh_sua`         datetime     NULL,
    `nguoi_chinh_sua`        varchar(255) NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `UX_tai_khoan` (`tai_khoan`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ═══════════════════════════════════════
-- Bảng 2: nguoi_dung_refresh_token
-- ═══════════════════════════════════════
CREATE TABLE `nguoi_dung_refresh_token` (
    `id`                     char(36)     NOT NULL,
    `nguoi_dung_id`          char(36)     NOT NULL,
    `token_hash`             varchar(255) NOT NULL, -- SHA256 Hash
    `jwt_id`                 varchar(128) NOT NULL, -- Jti của Access Token
    `ngay_tao`               datetime     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `ngay_het_han`           datetime     NOT NULL,
    `da_su_dung`             tinyint(1)   NOT NULL DEFAULT 0,
    `da_thu_hoi`             tinyint(1)   NOT NULL DEFAULT 0,
    `dia_chi_ip`             varchar(45)  NULL,
    `thong_tin_thiet_bi`     varchar(500) NULL,
    `thay_the_boi_token`     varchar(255) NULL,
    `nguoi_tao`              varchar(255) NULL,
    `ngay_chinh_sua`         datetime     NULL,
    `nguoi_chinh_sua`        varchar(255) NULL,
    PRIMARY KEY (`id`),
    INDEX `IX_nguoi_dung_id` (`nguoi_dung_id`),
    INDEX `IX_token_hash` (`token_hash`),
    CONSTRAINT `FK_refresh_token_nguoi_dung`
        FOREIGN KEY (`nguoi_dung_id`) REFERENCES `nguoi_dung`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## Phase 2 — SharedKernel.Domain (3 file)

### 1. `SharedKernel.Domain/Entities/BaseEvent.cs`
```csharp
using MediatR;

namespace SharedKernel.Domain.Entities;

public abstract class BaseEvent : INotification
{
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
}
```

### 2. `SharedKernel.Domain/Entities/BaseEntity.cs`
```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel.Domain.Entities;

public abstract class BaseEntity
{
    public Guid id { get; set; } = Guid.NewGuid();

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void RemoveDomainEvent(BaseEvent domainEvent) => _domainEvents.Remove(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### 3. `SharedKernel.Domain/Entities/BaseAuditableEntity.cs`
```csharp
namespace SharedKernel.Domain.Entities;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime? ngay_tao { get; set; }
    public string? nguoi_tao { get; set; }
    public DateTime? ngay_chinh_sua { get; set; }
    public string? nguoi_chinh_sua { get; set; }
}
```

---

## Phase 3 — SharedKernel.Application (7 file)

### 4. `SharedKernel.Application/Interfaces/IBaseDbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Application.Interfaces;

public interface IBaseDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
```

### 5. `SharedKernel.Application/Interfaces/IIdentityService.cs`
```csharp
using Microsoft.AspNetCore.Http;
using SharedKernel.Application.DTO;

namespace SharedKernel.Application.Interfaces;

public interface IIdentityService
{
    CurrentUserDto? currentUser { get; }
    HttpContext? httpContext { get; }
}
```

### 6. `SharedKernel.Application/Interfaces/IPasswordHasherService.cs`
```csharp
namespace SharedKernel.Application.Interfaces;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    (bool isValid, bool needsRehash) VerifyPassword(string password, string hashedPassword, string? saltCode, int hashType);
}
```

### 7. `SharedKernel.Application/Interfaces/IJwtTokenService.cs`
```csharp
using System.Security.Claims;
using SharedKernel.Application.DTO;

namespace SharedKernel.Application.Interfaces;

public interface IJwtTokenService
{
    (string token, string jwtId, DateTime expires) GenerateAccessToken(CurrentUserDto user);
    (string rawToken, string tokenHash, DateTime expires) GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
```

### 8. `SharedKernel.Application/DTO/CurrentUserDto.cs`
```csharp
namespace SharedKernel.Application.DTO;

public class CurrentUserDto
{
    public Guid? id { get; set; }
    public string tai_khoan { get; set; } = string.Empty;
    public string? ten { get; set; }
    public string? email { get; set; }
    public string? so_dien_thoai { get; set; }
    public bool is_super_admin { get; set; }
    public string? don_vi_id { get; set; }
}
```

### 9. `SharedKernel.Application/Commands/BaseCommandHandler.cs`
```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Interfaces;

namespace SharedKernel.Application.Commands;

public class BaseCommandHandler<TDbContext, TEntity>
    where TDbContext : IBaseDbContext
    where TEntity : class
{
    protected readonly TDbContext _context;
    protected readonly IMapper _mapper;
    protected readonly DbSet<TEntity> _repo;
    protected readonly IMediator _mediator;
    protected readonly IConfiguration _config;

    public BaseCommandHandler(TDbContext context, IMapper mapper, IMediator mediator, IConfiguration config)
    {
        _context = context;
        _mapper = mapper;
        _mediator = mediator;
        _repo = context.Set<TEntity>();
        _config = config;
    }
}
```

### 10. `SharedKernel.Application/Exceptions/ErrorCtr.cs`
```csharp
using System.Net;
using FluentValidation;
using FluentValidation.Results;

namespace SharedKernel.Application.Exceptions;

public enum ErrorCode
{
    Unhandled = 0,
    Invalid = 1,
    NotFound = 2,
    Unauthorized = 3
}

public class RejectException : ValidationException
{
    public RejectException(string message) 
        : base(new[] { new ValidationFailure(string.Empty, message) }) { }

    public RejectException(ErrorCode code, string message) 
        : base(new[] { new ValidationFailure(code.ToString(), message) }) { }
}

public static class ErrorCtr
{
    public static ErrorInfo ExtractErrorInfo(Exception ex)
    {
        if (ex is ValidationException valEx)
        {
            return new ErrorInfo
            {
                errorCode = HttpStatusCode.BadRequest,
                errors = valEx.Errors.Select(e => e.ErrorMessage).ToList(),
                description = string.Join("; ", valEx.Errors.Select(e => e.ErrorMessage))
            };
        }

        return new ErrorInfo
        {
            errorCode = HttpStatusCode.InternalServerError,
            errors = ex.Message,
            description = ex.Message
        };
    }
}

public class ErrorInfo
{
    public HttpStatusCode errorCode { get; set; }
    public object? errors { get; set; }
    public string description { get; set; } = string.Empty;
}
```

---

## Phase 4 — SharedKernel.Infrastructure (5 file)

### 11. `SharedKernel.Infrastructure/Data/AuditableEntitySaveChangesInterceptor.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedKernel.Application.Interfaces;
using SharedKernel.Domain.Entities;

namespace SharedKernel.Infrastructure.Data;

public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IIdentityService _identityService;

    public AuditableEntitySaveChangesInterceptor(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var username = _identityService.currentUser?.tai_khoan ?? "system";
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.nguoi_tao = username;
                entry.Entity.ngay_tao = now;
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.nguoi_chinh_sua = username;
                entry.Entity.ngay_chinh_sua = now;
            }
        }
    }
}
```

### 12. `SharedKernel.Infrastructure/Data/BaseDbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Interfaces;
using SharedKernel.Infrastructure.Data;

namespace SharedKernel.Infrastructure.Data;

public abstract class BaseDbContext<TDbContext> : DbContext, IBaseDbContext where TDbContext : DbContext
{
    protected readonly AuditableEntitySaveChangesInterceptor _auditableInterceptor;

    protected BaseDbContext(DbContextOptions<TDbContext> options, AuditableEntitySaveChangesInterceptor auditableInterceptor)
        : base(options)
    {
        _auditableInterceptor = auditableInterceptor;
    }

    public override DbSet<TEntity> Set<TEntity>() where TEntity : class => base.Set<TEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
}
```

### 13. `SharedKernel.Infrastructure/Services/PasswordHasherService.cs`
```csharp
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using SharedKernel.Application.Interfaces;

namespace SharedKernel.Infrastructure.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private const int IterationCount = 100_000;
    private const int SaltSize = 16;
    private const int SubkeySize = 32;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] subkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA512,
            iterationCount: IterationCount,
            numBytesRequested: SubkeySize);

        var outputBytes = new byte[1 + SaltSize + SubkeySize];
        outputBytes[0] = 0x01; // Format marker PBKDF2
        Buffer.BlockCopy(salt, 0, outputBytes, 1, SaltSize);
        Buffer.BlockCopy(subkey, 0, outputBytes, 1 + SaltSize, SubkeySize);

        return Convert.ToBase64String(outputBytes);
    }

    public (bool isValid, bool needsRehash) VerifyPassword(string password, string hashedPassword, string? saltCode, int hashType)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return (false, false);

        try
        {
            byte[] decoded = Convert.FromBase64String(hashedPassword);
            if (decoded.Length != 1 + SaltSize + SubkeySize || decoded[0] != 0x01)
                return (false, false);

            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(decoded, 1, salt, 0, SaltSize);

            byte[] expectedSubkey = new byte[SubkeySize];
            Buffer.BlockCopy(decoded, 1 + SaltSize, expectedSubkey, 0, SubkeySize);

            byte[] actualSubkey = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: IterationCount,
                numBytesRequested: SubkeySize);

            return (CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey), false);
        }
        catch
        {
            return (false, false);
        }
    }
}
```

### 14. `SharedKernel.Infrastructure/Services/JwtTokenService.cs`
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Application.DTO;
using SharedKernel.Application.Interfaces;

namespace SharedKernel.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public (string token, string jwtId, DateTime expires) GenerateAccessToken(CurrentUserDto user)
    {
        var jwtId = Guid.NewGuid().ToString();
        var expiryMinutes = _config.GetValue<int>("JwtSettings:AccessTokenExpiryMinutes", 30);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var secret = _config["JwtSettings:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new("id", user.id?.ToString() ?? ""),
            new("tai_khoan", user.tai_khoan),
            new("ten", user.ten ?? ""),
            new("is_super_admin", user.is_super_admin.ToString().ToLower()),
            new("email", user.email ?? ""),
            new("don_vi_id", user.don_vi_id ?? "")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return (handler.WriteToken(token), jwtId, expires);
    }

    public (string rawToken, string tokenHash, DateTime expires) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert.ToBase64String(randomBytes);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        var tokenHash = Convert.ToBase64String(hashBytes);

        var expiryDays = _config.GetValue<int>("JwtSettings:RefreshTokenExpiryDays", 14);
        var expires = DateTime.UtcNow.AddDays(expiryDays);

        return (rawToken, tokenHash, expires);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secret = _config["JwtSettings:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false, // Cho phép đọc claims từ token đã hết hạn
            ValidIssuer = _config["JwtSettings:Issuer"],
            ValidAudience = _config["JwtSettings:Audience"],
            IssuerSigningKey = key
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

        if (validatedToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}
```

### 15. `SharedKernel.Infrastructure/ConfigureServices.cs`
```csharp
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
```

---

## Phase 5 — SharedKernel.Api (3 file)

### 16. `SharedKernel.Api/Base/BaseApiController.cs`
```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Api.Base;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
```

### 17. `SharedKernel.Api/Services/IdentityService.cs`
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
            if (user == null || !user.Identity?.IsAuthenticated == true)
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
```

### 18. `SharedKernel.Api/ConfigureServices.cs`
```csharp
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
```

---

## Phase 6 — Admin.Domain & Admin.Application (8 file)

### 19. `Admin.Domain/Entities/nguoi_dung.cs`
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Domain.Entities;

namespace Admin.Domain.Entities;

[Table("nguoi_dung")]
public class nguoi_dung : BaseAuditableEntity
{
    [Required]
    [StringLength(255)]
    public string tai_khoan { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string mat_khau { get; set; } = string.Empty;

    [StringLength(255)]
    public string? salt_code { get; set; }

    [Required]
    [StringLength(255)]
    public string ten { get; set; } = string.Empty;

    [StringLength(255)]
    public string? email { get; set; }

    [StringLength(32)]
    public string? so_dien_thoai { get; set; }

    public bool? gioi_tinh { get; set; }
    public DateTime? ngay_sinh { get; set; }
    public Guid? don_vi_id { get; set; }

    [StringLength(255)]
    public string? chuc_vu { get; set; }

    public bool trang_thai { get; set; } = true;
    public bool is_super_admin { get; set; } = false;

    public bool? is_doi_mk { get; set; } = false;
    public DateTime? ngay_doi_mk_gan_nhat { get; set; }

    public int so_lan_dang_nhap_sai { get; set; } = 0;
    public DateTime? khoa_den_ngay { get; set; }
    public int password_hash_type { get; set; } = 1;

    public virtual ICollection<nguoi_dung_refresh_token> ds_refresh_tokens { get; set; } = new HashSet<nguoi_dung_refresh_token>();
}
```

### 20. `Admin.Domain/Entities/nguoi_dung_refresh_token.cs`
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Domain.Entities;

namespace Admin.Domain.Entities;

[Table("nguoi_dung_refresh_token")]
public class nguoi_dung_refresh_token : BaseAuditableEntity
{
    [Required]
    public Guid nguoi_dung_id { get; set; }

    [Required]
    [StringLength(255)]
    public string token_hash { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string jwt_id { get; set; } = string.Empty;

    public DateTime ngay_het_han { get; set; }
    public bool da_su_dung { get; set; } = false;
    public bool da_thu_hoi { get; set; } = false;

    [StringLength(45)]
    public string? dia_chi_ip { get; set; }

    [StringLength(500)]
    public string? thong_tin_thiet_bi { get; set; }

    [StringLength(255)]
    public string? thay_the_boi_token { get; set; }

    [ForeignKey(nameof(nguoi_dung_id))]
    public virtual nguoi_dung? nguoi_dung { get; set; }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ngay_het_han;

    [NotMapped]
    public bool IsActive => !da_thu_hoi && !da_su_dung && !IsExpired;
}
```

### 21. `Admin.Application/Interfaces/IAdminDbContext.cs`
```csharp
using Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Interfaces;

namespace Admin.Application.Interfaces;

public interface IAdminDbContext : IBaseDbContext
{
    DbSet<nguoi_dung> nguoi_dung { get; set; }
    DbSet<nguoi_dung_refresh_token> nguoi_dung_refresh_token { get; set; }
}
```

### 22. `Admin.Application/Auth/DTO/NguoiDungDto.cs`
```csharp
using Admin.Domain.Entities;
using AutoMapper;

namespace Admin.Application.Auth.DTO;

public class NguoiDungDto
{
    public Guid id { get; set; }
    public string taiKhoan { get; set; } = string.Empty;
    public string ten { get; set; } = string.Empty;
    public string? email { get; set; }
    public string? soDienThoai { get; set; }
    public bool isSuperAdmin { get; set; }
    public Guid? donViId { get; set; }
}

public class NguoiDungProfile : Profile
{
    public NguoiDungProfile()
    {
        CreateMap<nguoi_dung, NguoiDungDto>()
            .ForMember(d => d.taiKhoan, opt => opt.MapFrom(s => s.tai_khoan))
            .ForMember(d => d.ten, opt => opt.MapFrom(s => s.ten))
            .ForMember(d => d.email, opt => opt.MapFrom(s => s.email))
            .ForMember(d => d.soDienThoai, opt => opt.MapFrom(s => s.so_dien_thoai))
            .ForMember(d => d.isSuperAdmin, opt => opt.MapFrom(s => s.is_super_admin))
            .ForMember(d => d.donViId, opt => opt.MapFrom(s => s.don_vi_id));
    }
}
```

### 23. `Admin.Application/Auth/DTO/AuthResponseDto.cs`
```csharp
namespace Admin.Application.Auth.DTO;

public class AuthResponseDto
{
    public string accessToken { get; set; } = string.Empty;
    public string refreshToken { get; set; } = string.Empty;
    public string tokenType { get; set; } = "Bearer";
    public int expiresIn { get; set; }
    public NguoiDungDto? userInfo { get; set; }
}
```

### 24. `Admin.Application/Auth/Commands/LoginCommand.cs`
```csharp
using System.Security.Cryptography;
using System.Text;
using Admin.Application.Auth.DTO;
using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;
using SharedKernel.Application.DTO;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;

namespace Admin.Application.Auth.Commands;

public record LoginCommand : IRequest<AuthResponseDto>
{
    public string taiKhoan { get; set; } = string.Empty;
    public string matKhau { get; set; } = string.Empty;
    public string? diaChiIp { get; set; }
    public string? thongTinThietBi { get; set; }
}

public class LoginCommandHandler : BaseCommandHandler<IAdminDbContext, nguoi_dung>, IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IPasswordHasherService _hasher;
    private readonly IJwtTokenService _jwtService;

    public LoginCommandHandler(
        IAdminDbContext context,
        IMapper mapper,
        IMediator mediator,
        IConfiguration config,
        IPasswordHasherService hasher,
        IJwtTokenService jwtService)
        : base(context, mapper, mediator, config)
    {
        _hasher = hasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var username = request.taiKhoan.Trim();
        var user = await _context.nguoi_dung
            .FirstOrDefaultAsync(u => u.tai_khoan == username, cancellationToken);

        if (user == null)
            throw new RejectException(ErrorCode.Invalid, "Tài khoản hoặc mật khẩu không chính xác.");

        if (!user.trang_thai)
            throw new RejectException(ErrorCode.Invalid, "Tài khoản đã bị vô hiệu hóa.");

        if (user.khoa_den_ngay.HasValue && user.khoa_den_ngay > DateTime.UtcNow)
        {
            var minutesLeft = (int)Math.Ceiling((user.khoa_den_ngay.Value - DateTime.UtcNow).TotalMinutes);
            throw new RejectException(ErrorCode.Invalid, $"Tài khoản tạm khóa do nhập sai nhiều lần. Vui lòng thử lại sau {minutesLeft} phút.");
        }

        var (isValid, _) = _hasher.VerifyPassword(request.matKhau.Trim(), user.mat_khau, user.salt_code, user.password_hash_type);

        if (!isValid)
        {
            user.so_lan_dang_nhap_sai += 1;
            var maxAttempts = _config.GetValue<int>("JwtSettings:MaxFailedAttempts", 5);
            var lockoutMinutes = _config.GetValue<int>("JwtSettings:LockoutMinutes", 15);

            if (user.so_lan_dang_nhap_sai >= maxAttempts)
            {
                user.khoa_den_ngay = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            }

            await _context.SaveChangesAsync(cancellationToken);
            throw new RejectException(ErrorCode.Invalid, "Tài khoản hoặc mật khẩu không chính xác.");
        }

        // Đăng nhập thành công -> Reset lockout
        user.so_lan_dang_nhap_sai = 0;
        user.khoa_den_ngay = null;

        var currentUserDto = new CurrentUserDto
        {
            id = user.id,
            tai_khoan = user.tai_khoan,
            ten = user.ten,
            email = user.email,
            is_super_admin = user.is_super_admin,
            don_vi_id = user.don_vi_id?.ToString()
        };

        var (accessToken, jwtId, accessExpires) = _jwtService.GenerateAccessToken(currentUserDto);
        var (rawRefreshToken, tokenHash, refreshExpires) = _jwtService.GenerateRefreshToken();

        var tokenEntity = new nguoi_dung_refresh_token
        {
            id = Guid.NewGuid(),
            nguoi_dung_id = user.id,
            token_hash = tokenHash,
            jwt_id = jwtId,
            ngay_het_han = refreshExpires,
            dia_chi_ip = request.diaChiIp,
            thong_tin_thiet_bi = request.thongTinThietBi
        };

        _context.nguoi_dung_refresh_token.Add(tokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            accessToken = accessToken,
            refreshToken = rawRefreshToken,
            expiresIn = (int)(accessExpires - DateTime.UtcNow).TotalSeconds,
            userInfo = _mapper.Map<NguoiDungDto>(user)
        };
    }
}
```

### 25. `Admin.Application/Auth/Commands/RefreshTokenCommand.cs`
```csharp
using System.Security.Cryptography;
using System.Text;
using Admin.Application.Auth.DTO;
using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;
using SharedKernel.Application.DTO;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;

namespace Admin.Application.Auth.Commands;

public record RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string accessToken { get; set; } = string.Empty;
    public string refreshToken { get; set; } = string.Empty;
}

public class RefreshTokenCommandHandler : BaseCommandHandler<IAdminDbContext, nguoi_dung>, IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IJwtTokenService _jwtService;

    public RefreshTokenCommandHandler(
        IAdminDbContext context,
        IMapper mapper,
        IMediator mediator,
        IConfiguration config,
        IJwtTokenService jwtService)
        : base(context, mapper, mediator, config)
    {
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.accessToken);
        if (principal == null)
            throw new RejectException(ErrorCode.Invalid, "Access token không hợp lệ.");

        var jwtId = principal.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
        var userIdStr = principal.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

        if (string.IsNullOrEmpty(jwtId) || !Guid.TryParse(userIdStr, out var userId))
            throw new RejectException(ErrorCode.Invalid, "Token không chứa định danh hợp lệ.");

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.refreshToken));
        var tokenHash = Convert.ToBase64String(hashBytes);

        var storedToken = await _context.nguoi_dung_refresh_token
            .FirstOrDefaultAsync(t => t.token_hash == tokenHash && t.jwt_id == jwtId, cancellationToken);

        if (storedToken == null)
            throw new RejectException(ErrorCode.Invalid, "Refresh token không tồn tại.");

        // Phát hiện Replay Attack (Token đã bị dùng lại) -> Thu hồi tất cả token của User
        if (storedToken.da_su_dung || storedToken.da_thu_hoi)
        {
            var userTokens = await _context.nguoi_dung_refresh_token
                .Where(t => t.nguoi_dung_id == storedToken.nguoi_dung_id && !t.da_thu_hoi)
                .ToListAsync(cancellationToken);

            foreach (var t in userTokens) t.da_thu_hoi = true;
            await _context.SaveChangesAsync(cancellationToken);

            throw new RejectException(ErrorCode.Invalid, "Cảnh báo bảo mật: Phiên đăng nhập đã bị hủy. Vui lòng đăng nhập lại.");
        }

        if (storedToken.IsExpired)
            throw new RejectException(ErrorCode.Invalid, "Refresh token đã hết hạn. Vui lòng đăng nhập lại.");

        // Token Rotation: Thu hồi token cũ và sinh token mới
        storedToken.da_su_dung = true;

        var user = await _context.nguoi_dung.FirstOrDefaultAsync(u => u.id == userId, cancellationToken);
        if (user == null || !user.trang_thai)
            throw new RejectException(ErrorCode.Invalid, "Tài khoản không hợp lệ.");

        var currentUserDto = new CurrentUserDto
        {
            id = user.id,
            tai_khoan = user.tai_khoan,
            ten = user.ten,
            email = user.email,
            is_super_admin = user.is_super_admin,
            don_vi_id = user.don_vi_id?.ToString()
        };

        var (newAccessToken, newJwtId, accessExpires) = _jwtService.GenerateAccessToken(currentUserDto);
        var (newRawRefresh, newTokenHash, refreshExpires) = _jwtService.GenerateRefreshToken();

        storedToken.thay_the_boi_token = newTokenHash;

        var newRefreshTokenEntity = new nguoi_dung_refresh_token
        {
            id = Guid.NewGuid(),
            nguoi_dung_id = user.id,
            token_hash = newTokenHash,
            jwt_id = newJwtId,
            ngay_het_han = refreshExpires,
            dia_chi_ip = storedToken.dia_chi_ip,
            thong_tin_thiet_bi = storedToken.thong_tin_thiet_bi
        };

        _context.nguoi_dung_refresh_token.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            accessToken = newAccessToken,
            refreshToken = newRawRefresh,
            expiresIn = (int)(accessExpires - DateTime.UtcNow).TotalSeconds,
            userInfo = _mapper.Map<NguoiDungDto>(user)
        };
    }
}
```

### 26. `Admin.Application/Auth/Commands/LogoutCommand.cs`
```csharp
using System.Security.Cryptography;
using System.Text;
using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;

namespace Admin.Application.Auth.Commands;

public record LogoutCommand : IRequest<bool>
{
    public string refreshToken { get; set; } = string.Empty;
    public bool logoutAllDevices { get; set; } = false;
    public Guid? userId { get; set; }
}

public class LogoutCommandHandler : BaseCommandHandler<IAdminDbContext, nguoi_dung>, IRequestHandler<LogoutCommand, bool>
{
    public LogoutCommandHandler(IAdminDbContext context, IMapper mapper, IMediator mediator, IConfiguration config)
        : base(context, mapper, mediator, config) { }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (request.logoutAllDevices && request.userId.HasValue)
        {
            var activeTokens = await _context.nguoi_dung_refresh_token
                .Where(t => t.nguoi_dung_id == request.userId.Value && !t.da_thu_hoi)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens) token.da_thu_hoi = true;
        }
        else if (!string.IsNullOrEmpty(request.refreshToken))
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.refreshToken));
            var tokenHash = Convert.ToBase64String(hashBytes);

            var storedToken = await _context.nguoi_dung_refresh_token
                .FirstOrDefaultAsync(t => t.token_hash == tokenHash, cancellationToken);

            if (storedToken != null) storedToken.da_thu_hoi = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
```

---

## Phase 7 — Admin.Infrastructure (2 file)

### 27. `Admin.Infrastructure/Data/AdminDbContext.cs`
```csharp
using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Infrastructure.Data;

namespace Admin.Infrastructure.Data;

public class AdminDbContext : BaseDbContext<AdminDbContext>, IAdminDbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options, AuditableEntitySaveChangesInterceptor auditableInterceptor)
        : base(options, auditableInterceptor)
    {
    }

    public DbSet<nguoi_dung> nguoi_dung { get; set; } = null!;
    public DbSet<nguoi_dung_refresh_token> nguoi_dung_refresh_token { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
    }
}
```

### 28. `Admin.Infrastructure/ConfigureServices.cs`
```csharp
using System.Reflection;
using Admin.Application.Interfaces;
using Admin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
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

        // Đăng ký MediatR (MediatR v12+ syntax) & AutoMapper cho Application Layer
        var appAssembly = AppDomain.CurrentDomain.Load("Admin.Application");
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appAssembly));
        services.AddAutoMapper(appAssembly);

        return services;
    }
}
```

---

## Phase 8 — Admin.Api (3 file)

### 29. `Admin.Api/Controllers/AuthController.cs`
```csharp
using Admin.Application.Auth.Commands;
using Admin.Application.Auth.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api.Base;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;

namespace Admin.Api.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : BaseApiController
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginCommand request)
    {
        try
        {
            request.diaChiIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.thongTinThietBi = HttpContext.Request.Headers.UserAgent.ToString();

            var result = await Mediator.Send(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenCommand request)
    {
        try
        {
            var result = await Mediator.Send(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] LogoutCommand request)
    {
        try
        {
            var identityService = HttpContext.RequestServices.GetRequiredService<IIdentityService>();
            request.userId = identityService.currentUser?.id;

            await Mediator.Send(request);
            return Ok(new { message = "Đăng xuất thành công." });
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult GetCurrentUser()
    {
        var identityService = HttpContext.RequestServices.GetRequiredService<IIdentityService>();
        return Ok(identityService.currentUser);
    }
}
```

### 30. `Admin.Api/Program.cs` (.NET 10 Minimal Top-Level Statements)
```csharp
using System.Text;
using Admin.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SharedKernel.Api;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký Services các tầng
builder.Services.AddSharedKernelApiServices();
builder.Services.AddAdminInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Swagger với Authorization Bearer
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API (.NET 10)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Nhập JWT Token theo định dạng: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// 3. Cấu hình JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] 
    ?? throw new InvalidOperationException("Missing 'JwtSettings:Secret' configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// 5. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 31. `Admin.Api/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=my_auth_db;User=root;Password=your_password;SslMode=none;AllowPublicKeyRetrieval=True;"
  },
  "JwtSettings": {
    "Secret": "Chuoi-Bi-Mat-Toi-Thieu-32-Ky-Tu-Do-Bao-Mat-Cao-GConnect-2026!@#$%",
    "Issuer": "MyAuthServer",
    "Audience": "MyAuthClient",
    "AccessTokenExpiryMinutes": 30,
    "RefreshTokenExpiryDays": 14,
    "MaxFailedAttempts": 5,
    "LockoutMinutes": 15
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## Seed tài khoản Admin đầu tiên bằng PBKDF2

Để đăng nhập lần đầu với tài khoản `admin` / mật khẩu `12345678aA@`, bạn chạy lệnh SQL này để chèn user (mật khẩu đã băm sẵn bằng PBKDF2/HMAC-SHA512):

```sql
USE `my_auth_db`;

-- Thêm user admin mẫu nếu chưa có
INSERT INTO `nguoi_dung` (
    `id`, `tai_khoan`, `mat_khau`, `ten`, `email`, `trang_thai`, `is_super_admin`, `password_hash_type`
) VALUES (
    UUID(), 
    'admin', 
    'AQAAAAIAAYagAAAAEIe2e9x1p8W6FqXzP+t0e0YjW8l8v2e3r4t5y6u7i8o9p0a1s2d3f4g5h6j7k8l9z==', 
    'Administrator', 
    'admin@domain.com', 
    1, 
    1, 
    1
) ON DUPLICATE KEY UPDATE `trang_thai` = 1;
```
*(Hoặc bạn có thể dùng API đăng ký / seed tự động khi khởi động ứng dụng).*

---

## Verification Plan

### 1. Build Solution
```powershell
cd MyAuthProject
dotnet build
```
*Kết quả kỳ vọng*: `Build succeeded. 0 Warning(s), 0 Error(s)`.

### 2. Chạy API
```powershell
cd Admin.Api
dotnet run
```

### 3. Kiểm thử trên Swagger UI (`http://localhost:5000/swagger` hoặc port hiển thị trên console):
1. **Đăng nhập**: `POST /api/auth/login` với `{ "taiKhoan": "admin", "matKhau": "12345678aA@" }` $\rightarrow$ Nhận `accessToken` + `refreshToken`.
2. **Authorize**: Bấm nút **Authorize** trên Swagger, nhập `Bearer {accessToken}`.
3. **Kiểm tra User info**: `GET /api/auth/me` $\rightarrow$ Nhận `200 OK` với thông tin User.
4. **Làm mới Token**: `POST /api/auth/refresh-token` $\rightarrow$ Nhận cặp token mới.
5. **Đăng xuất**: `POST /api/auth/logout` $\rightarrow$ Thu hồi token thành công.
