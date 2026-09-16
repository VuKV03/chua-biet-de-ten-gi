# Hướng dẫn tự triển khai: tách service **Todo** riêng (CRUD Task)

## Context

Hiện repo mới có 1 module nghiệp vụ là **Admin** (auth/user) — thứ bạn xác định là "tính năng chung".
Tính năng Todo là nghiệp vụ riêng nên **không nhét vào Admin**, mà dựng thành **service độc lập**:
4 project mới (`Todo.Domain/Application/Infrastructure/Api`) với **host riêng chạy port riêng**.

Tài liệu này để **bạn tự gõ code**: làm tuần tự Phase 0 → 7, mỗi file có code đầy đủ + giải thích *tại sao*.

### Quyết định đã chốt

| Vấn đề | Chốt |
|---|---|
| Mức độ tách | **Service riêng, host riêng** — Admin.Api (`:5043`) giữ nguyên 100%, Todo.Api (`:5100`) hoàn toàn mới |
| Tổ chức solution | **3 nhóm bằng Solution Folder**: `SharedKernel` / `Admin` / `Todo` — nhóm ảo trong `.slnx`, file trên đĩa **giữ nguyên vị trí phẳng**, không đụng `ProjectReference` |
| Tên module | `Todo.*` |
| Database | **Chung `my_auth_db`**, nhưng `TodoDbContext` riêng, bảng riêng, **không FK cứng** sang `nguoi_dung` |
| Quy ước tên | DB + Domain tiếng Việt (`cong_viec`, `tieu_de`, `trang_thai`, `ngay_hoan_thanh`); Application + API tiếng Anh (`TodoDto`, `CreateTodoCommand`, route `api/todo`) — DTO lo việc dịch tên, y hệt `nguoi_dung` → `NguoiDungDto.taiKhoan` |
| `status` | Enum: `ChuaLam=0`, `DangLam=1`, `HoanThanh=2` |
| Quyền | `[Authorize]` toàn bộ, mỗi user chỉ CRUD todo của mình |
| Danh sách | Phân trang: `?status=&page=&pageSize=` → `{ items, totalCount, page, pageSize, totalPages }` |

### Sơ đồ sau khi xong

**Trong IDE** (Visual Studio / Rider) — nhóm bằng Solution Folder, chia 3 khối rõ ràng:

```
api-net10.slnx
├─ 📁 SharedKernel/                 (hạ tầng dùng chung, không phải service)
│   ├─ SharedKernel.Domain
│   ├─ SharedKernel.Application     ← THÊM: PagedResult, BaseQueryHandler
│   ├─ SharedKernel.Infrastructure
│   └─ SharedKernel.Api             ← THÊM: HostConfigurationExtensions (JWT + Swagger dùng chung)
│
├─ 📁 Admin/                        ← SERVICE 1 — auth, user (tính năng chung)
│   ├─ Admin.Domain                 ← GIỮ NGUYÊN, không sửa 1 dòng nào
│   ├─ Admin.Application            ← GIỮ NGUYÊN
│   ├─ Admin.Infrastructure         ← GIỮ NGUYÊN
│   └─ Admin.Api          :5043     ← GIỮ NGUYÊN
│
└─ 📁 Todo/                         ← SERVICE 2 — nghiệp vụ todo (MỚI HOÀN TOÀN)
    ├─ Todo.Domain
    ├─ Todo.Application
    ├─ Todo.Infrastructure
    └─ Todo.Api           :5100     ← host độc lập
```

**Trên đĩa** — 12 thư mục vẫn nằm phẳng ở gốc repo, **không di chuyển file nào**:

```
api-net10/
├─ Admin.Api/  Admin.Application/  Admin.Domain/  Admin.Infrastructure/
├─ SharedKernel.Api/  SharedKernel.Application/  SharedKernel.Domain/  SharedKernel.Infrastructure/
├─ Todo.Api/  Todo.Application/  Todo.Domain/  Todo.Infrastructure/
├─ Database/   scripts/
└─ api-net10.slnx        ← chỉ file này khai báo cách nhóm
```

> Chọn cách này (nhóm ảo) thay vì di chuyển thư mục thật vì: **rủi ro bằng 0** — không phải sửa đường dẫn
> `ProjectReference` trong 8 file `.csproj` đang chạy tốt, không phải sửa lệnh `dotnet run --project ...`,
> không phải sửa link trong `CLAUDE.md`. Mọi lệnh `dotnet` hiện có vẫn chạy y nguyên.

### Hai service nói chuyện với nhau thế nào?

**Không hề gọi HTTP sang nhau.** JWT là stateless: `Todo.Api` chỉ cần cấu hình **cùng `Secret`/`Issuer`/`Audience`**
là tự verify được chữ ký của token do `Admin.Api` cấp, rồi đọc `id`/`tai_khoan` từ claim trong token —
qua đúng `IIdentityService` có sẵn ở `SharedKernel.Api` (nó đọc từ `HttpContext.User`, **không truy vấn DB**).

```
POST :5043/api/auth/login  →  accessToken
POST :5100/api/todo        →  gửi kèm chính accessToken đó  →  OK
```

> ⚠️ **Ràng buộc vận hành**: `JwtSettings:Secret` phải **giống hệt** ở 2 appsettings.json. Đổi 1 bên
> mà quên bên kia là toàn bộ token bị từ chối ở service kia. Lên production nên đẩy secret ra biến
> môi trường/Key Vault dùng chung thay vì chép tay 2 chỗ.

### 4 thứ đã có sẵn — không code lại

1. **`createdAt`/`updatedAt` tự động**: `BaseAuditableEntity` + `AuditableEntitySaveChangesInterceptor`
   tự điền `ngay_tao`/`ngay_chinh_sua` mỗi lần `SaveChangesAsync`. **Đừng tự set trong handler.**
2. **`BaseDbContext<T>`** đã gắn sẵn interceptor đó — `TodoDbContext` chỉ cần kế thừa là xong.
3. **`ValidationBehaviour`** đã có ở SharedKernel, chỉ cần đăng ký lại trong `Todo.Infrastructure`
   (1 dòng `cfg.AddOpenBehavior`) là validator tự chạy trước handler, kể cả với Query.
4. **`ErrorCtr.ExtractErrorInfo`** map sẵn `NotFound`→404, `Unauthorized`→401, còn lại→400.

---

# Phase 0 — Tạo 4 project mới + gom Solution Folder

Chạy ở thư mục gốc repo:

```powershell
dotnet new classlib -n Todo.Domain          -f net10.0
dotnet new classlib -n Todo.Application     -f net10.0
dotnet new classlib -n Todo.Infrastructure  -f net10.0
dotnet new webapi   -n Todo.Api             -f net10.0

# --solution-folder: thêm thẳng vào nhóm "Todo" trong solution, khỏi phải sửa .slnx bằng tay
dotnet sln add Todo.Domain Todo.Application Todo.Infrastructure Todo.Api --solution-folder Todo

# Reference: y hệt chuỗi phụ thuộc của Admin.*
dotnet add Todo.Domain          reference SharedKernel.Domain
dotnet add Todo.Application     reference Todo.Domain SharedKernel.Application
dotnet add Todo.Infrastructure  reference Todo.Application SharedKernel.Infrastructure
dotnet add Todo.Api             reference Todo.Infrastructure SharedKernel.Api
```

### Gom 8 project cũ vào 2 nhóm SharedKernel / Admin

8 project hiện có đang nằm phẳng ở gốc `api-net10.slnx`. Mở file đó ra **sửa tay** (nhanh và an toàn hơn
chạy `dotnet sln remove` rồi add lại) — chỉ là bọc các dòng `<Project>` sẵn có vào trong thẻ `<Folder>`:

```xml
<Solution>
  <Folder Name="/SharedKernel/">
    <Project Path="SharedKernel.Domain/SharedKernel.Domain.csproj" />
    <Project Path="SharedKernel.Application/SharedKernel.Application.csproj" />
    <Project Path="SharedKernel.Infrastructure/SharedKernel.Infrastructure.csproj" />
    <Project Path="SharedKernel.Api/SharedKernel.Api.csproj" />
  </Folder>

  <Folder Name="/Admin/">
    <Project Path="Admin.Domain/Admin.Domain.csproj" />
    <Project Path="Admin.Application/Admin.Application.csproj" />
    <Project Path="Admin.Infrastructure/Admin.Infrastructure.csproj" />
    <Project Path="Admin.Api/Admin.Api.csproj" />
  </Folder>

  <Folder Name="/Todo/">
    <Project Path="Todo.Domain/Todo.Domain.csproj" />
    <Project Path="Todo.Application/Todo.Application.csproj" />
    <Project Path="Todo.Infrastructure/Todo.Infrastructure.csproj" />
    <Project Path="Todo.Api/Todo.Api.csproj" />
  </Folder>
</Solution>
```

**Giải thích cú pháp `.slnx`**:
- Tên folder **phải có dấu `/` ở đầu và cuối** (`"/Admin/"`) — đây là quy ước của định dạng `.slnx`.
- Thuộc tính `Path` của `<Project>` **vẫn tính từ vị trí file `.slnx`** (tức từ gốc repo), **không** tính
  từ thư mục cha — nên đường dẫn giữ nguyên như cũ, chỉ khác là dòng đó nằm bên trong thẻ `<Folder>`.
- Nếu bạn đã chạy lệnh `dotnet sln add ... --solution-folder Todo` ở trên thì khối `/Todo/` đã tự sinh ra,
  chỉ cần thêm tay 2 khối `/SharedKernel/` và `/Admin/`.

Sau khi sửa, chạy `dotnet build` để chắc solution vẫn đọc được (kỳ vọng: build xanh y như trước, vì
Solution Folder chỉ là cách hiển thị, **không ảnh hưởng gì tới việc biên dịch hay tham chiếu project**).

### ⚠️ Cài package PHẢI ghim version (quan trọng nhất Phase này)

```powershell
dotnet add Todo.Infrastructure package Pomelo.EntityFrameworkCore.MySql   --version 9.0.0
dotnet add Todo.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 9.0.19

dotnet add Todo.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.11
dotnet add Todo.Api package Swashbuckle.AspNetCore                        --version 10.2.3

# Cần cho HostConfigurationExtensions ở Phase 2 (SharedKernel.Api chưa có Swashbuckle)
dotnet add SharedKernel.Api package Swashbuckle.AspNetCore --version 10.2.3
```

**Vì sao phải `--version`**: nếu gõ `dotnet add package Microsoft.EntityFrameworkCore.Design` không kèm
version, NuGet kéo bản **10.x mới nhất** → tái hiện đúng lỗi runtime bạn vừa gặp:
`MissingMethodException: ...AbstractionsStrings.ArgumentIsEmpty...` (Pomelo mới nhất là 9.0.0, chỉ chạy
với EF Core 9.x). Cả solution đang ghim EF Core **9.0.19**, service mới phải theo.

### Dọn file mặc định

```powershell
del Todo.Domain\Class1.cs, Todo.Application\Class1.cs, Todo.Infrastructure\Class1.cs
```
`Todo.Api/Program.cs` (bản WeatherForecast mặc định) sẽ ghi đè ở Phase 6.

> Chạy `dotnet build` — phải xanh trước khi đi tiếp.

---

# Phase 1 — Bảng trong MySQL

### File mới: `Database/03_cong_viec.sql`

```sql
-- ═══════════════════════════════════════════════════════════
-- Bảng cong_viec — thuộc service Todo
-- Cùng database my_auth_db nhưng KHÔNG FK sang nguoi_dung:
-- 2 service độc lập, chỉ liên kết logic qua nguoi_dung_id.
-- Chạy SAU 01_schema.sql
-- ═══════════════════════════════════════════════════════════
USE `my_auth_db`;

CREATE TABLE `cong_viec` (
    `id`               char(36)     NOT NULL,
    `nguoi_dung_id`    char(36)     NOT NULL, -- id user lấy từ claim JWT, không ràng buộc FK
    `tieu_de`          varchar(500) NOT NULL,
    `trang_thai`       int          NOT NULL DEFAULT 0, -- 0: chưa làm, 1: đang làm, 2: hoàn thành
    `ngay_hoan_thanh`  datetime     NULL,
    `ngay_tao`         datetime     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `nguoi_tao`        varchar(255) NULL,
    `ngay_chinh_sua`   datetime     NULL,
    `nguoi_chinh_sua`  varchar(255) NULL,
    PRIMARY KEY (`id`),
    INDEX `IX_cong_viec_nguoi_dung_id` (`nguoi_dung_id`),
    INDEX `IX_cong_viec_trang_thai` (`trang_thai`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

**Giải thích**: 4 cột audit cuối là bắt buộc vì entity kế thừa `BaseAuditableEntity` — thiếu là EF Core
báo "Unknown column" lúc query. Không có FK nên **xóa user sẽ để lại todo mồ côi** — đánh đổi có chủ ý
để 2 service tách được thật; sau này muốn dọn thì thêm job/domain event, không nên thêm FK lại.

---

# Phase 2 — Bổ sung SharedKernel (thuần thêm mới, không sửa gì cũ)

### 2.1. File mới: `SharedKernel.Application/DTO/PagedResult.cs`

```csharp
namespace SharedKernel.Application.DTO;

public class PagedResult<T>
{
    public IReadOnlyList<T> items { get; set; } = [];
    public int totalCount { get; set; }
    public int page { get; set; }
    public int pageSize { get; set; }
    public int totalPages => pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
}
```

### 2.2. File mới: `SharedKernel.Application/Queries/BaseQueryHandler.cs`

```csharp
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Interfaces;

namespace SharedKernel.Application.Queries;

/// <summary>
/// Đối xứng với BaseCommandHandler nhưng cho luồng đọc (Query) của CQRS.
/// Khác: _repo là IQueryable đã AsNoTracking (nhanh hơn, EF khỏi theo dõi thay đổi),
/// và không có IMediator vì query không phát domain event.
/// </summary>
public class BaseQueryHandler<TDbContext, TEntity>
    where TDbContext : IBaseDbContext
    where TEntity : class
{
    protected readonly TDbContext _context;
    protected readonly IMapper _mapper;
    protected readonly IQueryable<TEntity> _repo;
    protected readonly IConfiguration _config;

    public BaseQueryHandler(TDbContext context, IMapper mapper, IConfiguration config)
    {
        _context = context;
        _mapper = mapper;
        _config = config;
        _repo = context.Set<TEntity>().AsNoTracking();
    }
}
```

### 2.3. File mới: `SharedKernel.Api/Extensions/HostConfigurationExtensions.cs`

Gom phần cấu hình host lặp lại giữa các service (JWT + Swagger) về một chỗ, để `Todo.Api/Program.cs`
không phải chép lại ~60 dòng của `Admin.Api/Program.cs`:

```csharp
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
```

**Lưu ý**: đây là file **thêm mới**, `Admin.Api/Program.cs` vẫn chạy với code inline cũ, không đụng tới.
Sau khi Todo chạy ổn, nếu muốn gọn thì mới rút Admin.Api sang dùng 2 extension này (đổi ~60 dòng lấy 2 dòng)
— việc tùy chọn, làm sau khi test 2 bên đều xanh.

---

# Phase 3 — Todo.Domain

### 3.1. File mới: `Todo.Domain/Enums/TrangThaiCongViec.cs`

```csharp
namespace Todo.Domain.Enums;

public enum TrangThaiCongViec
{
    ChuaLam = 0,
    DangLam = 1,
    HoanThanh = 2
}
```

### 3.2. File mới: `Todo.Domain/Entities/cong_viec.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Domain.Entities;
using Todo.Domain.Enums;

namespace Todo.Domain.Entities;

[Table("cong_viec")]
public class cong_viec : BaseAuditableEntity
{
    /// <summary>Id user lấy từ claim JWT. Cố tình KHÔNG có navigation property sang nguoi_dung:
    /// service Todo không được phụ thuộc vào Domain của Admin.</summary>
    [Required]
    public Guid nguoi_dung_id { get; set; }

    [Required]
    [StringLength(500)]
    public string tieu_de { get; set; } = string.Empty;

    public TrangThaiCongViec trang_thai { get; set; } = TrangThaiCongViec.ChuaLam;

    public DateTime? ngay_hoan_thanh { get; set; }
}
```

**Giải thích**:
- Kế thừa `BaseAuditableEntity` → có sẵn `id`, `ngay_tao`, `nguoi_tao`, `ngay_chinh_sua`, `nguoi_chinh_sua`.
- `trang_thai` khai kiểu **enum**: EF Core tự quy đổi sang cột `int`, còn code C# thì type-safe.
- **Không có** `[ForeignKey]`/navigation sang `nguoi_dung` (khác với `nguoi_dung_refresh_token` trong Admin) —
  đây chính là ranh giới giữa 2 service. `Todo.Domain` không reference `Admin.Domain`.

---

# Phase 4 — Todo.Application

Tạo các thư mục: `Interfaces/`, `DTO/`, `Commands/`, `Queries/`, `Validators/`.
(Service này chỉ có 1 aggregate nên không cần thêm cấp thư mục feature như `Auth/` bên Admin.)

### 4.1. `Todo.Application/Interfaces/ITodoDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Interfaces;
using Todo.Domain.Entities;

namespace Todo.Application.Interfaces;

public interface ITodoDbContext : IBaseDbContext
{
    DbSet<cong_viec> cong_viec { get; set; }
}
```

### 4.2. `Todo.Application/DTO/TodoDto.cs`

```csharp
using AutoMapper;
using Todo.Domain.Entities;

namespace Todo.Application.DTO;

public class TodoDto
{
    public Guid id { get; set; }
    public string title { get; set; } = string.Empty;
    public int status { get; set; }
    public DateTime? completedAt { get; set; }
    public DateTime? createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
}

public class TodoProfile : Profile
{
    public TodoProfile()
    {
        CreateMap<cong_viec, TodoDto>()
            .ForMember(d => d.title,       opt => opt.MapFrom(s => s.tieu_de))
            .ForMember(d => d.status,      opt => opt.MapFrom(s => (int)s.trang_thai))
            .ForMember(d => d.completedAt, opt => opt.MapFrom(s => s.ngay_hoan_thanh))
            .ForMember(d => d.createdAt,   opt => opt.MapFrom(s => s.ngay_tao))
            .ForMember(d => d.updatedAt,   opt => opt.MapFrom(s => s.ngay_chinh_sua));
    }
}
```

**Giải thích**: đây là chỗ "dịch" tên tiếng Việt của DB sang tên tiếng Anh của API — đúng khuôn
`NguoiDungDto.cs`. Để `Profile` chung file với DTO cũng theo khuôn đó. AutoMapper tự quét, khỏi đăng ký.

### 4.3. `Todo.Application/Commands/CreateTodoCommand.cs`

```csharp
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;
using Todo.Application.DTO;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;
using Todo.Domain.Enums;

namespace Todo.Application.Commands;

public record CreateTodoCommand : IRequest<TodoDto>
{
    public string title { get; set; } = string.Empty;
}

public class CreateTodoCommandHandler
    : BaseCommandHandler<ITodoDbContext, cong_viec>, IRequestHandler<CreateTodoCommand, TodoDto>
{
    private readonly IIdentityService _identityService;

    public CreateTodoCommandHandler(
        ITodoDbContext context,
        IMapper mapper,
        IMediator mediator,
        IConfiguration config,
        IIdentityService identityService)
        : base(context, mapper, mediator, config)
    {
        _identityService = identityService;
    }

    public async Task<TodoDto> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var userId = _identityService.currentUser?.id
            ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập.");

        var entity = new cong_viec
        {
            id = Guid.NewGuid(),
            nguoi_dung_id = userId,
            tieu_de = request.title.Trim(),
            trang_thai = TrangThaiCongViec.ChuaLam
        };

        _context.cong_viec.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Map SAU khi save: lúc này interceptor mới điền xong ngay_tao -> createdAt có giá trị
        return _mapper.Map<TodoDto>(entity);
    }
}
```

**Giải thích 2 điểm dễ sai**:
- **Lấy user từ `IIdentityService` trong handler**, không nhận `nguoiDungId` từ body. Command không có
  property đó thì client không thể giả mạo id người khác — an toàn hơn cách gán từ controller
  (`AuthController.Logout` đang làm), vì ở đây có tới 5 endpoint, chỉ cần quên 1 chỗ là thủng.
- **Gọi `_mapper.Map` sau `SaveChangesAsync`**, nếu map trước thì `createdAt` trả về `null`.

### 4.4. `Todo.Application/Commands/UpdateTodoCommand.cs`

```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;
using Todo.Application.DTO;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;
using Todo.Domain.Enums;

namespace Todo.Application.Commands;

public record UpdateTodoCommand : IRequest<TodoDto>
{
    public Guid id { get; set; }
    public string title { get; set; } = string.Empty;
    public int status { get; set; }
}

public class UpdateTodoCommandHandler
    : BaseCommandHandler<ITodoDbContext, cong_viec>, IRequestHandler<UpdateTodoCommand, TodoDto>
{
    private readonly IIdentityService _identityService;

    public UpdateTodoCommandHandler(
        ITodoDbContext context,
        IMapper mapper,
        IMediator mediator,
        IConfiguration config,
        IIdentityService identityService)
        : base(context, mapper, mediator, config)
    {
        _identityService = identityService;
    }

    public async Task<TodoDto> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var userId = _identityService.currentUser?.id
            ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập.");

        var entity = await _context.cong_viec
            .FirstOrDefaultAsync(x => x.id == request.id && x.nguoi_dung_id == userId, cancellationToken);

        if (entity == null)
            throw new RejectException(ErrorCode.NotFound, "Không tìm thấy công việc.");

        var trangThaiMoi = (TrangThaiCongViec)request.status;

        // Quy tắc completedAt
        if (trangThaiMoi == TrangThaiCongViec.HoanThanh && entity.trang_thai != TrangThaiCongViec.HoanThanh)
            entity.ngay_hoan_thanh = DateTime.UtcNow;   // vừa chuyển sang hoàn thành
        else if (trangThaiMoi != TrangThaiCongViec.HoanThanh)
            entity.ngay_hoan_thanh = null;              // rời khỏi trạng thái hoàn thành

        entity.tieu_de = request.title.Trim();
        entity.trang_thai = trangThaiMoi;

        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<TodoDto>(entity);
    }
}
```

**Giải thích**: lọc kèm `x.nguoi_dung_id == userId` ngay trong truy vấn → todo của người khác trả **404
chứ không phải 403**, để kẻ tấn công không dò được id nào tồn tại. `updatedAt` khỏi set, interceptor lo.

### 4.5. `Todo.Application/Commands/DeleteTodoCommand.cs`

```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Commands;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;

namespace Todo.Application.Commands;

public record DeleteTodoCommand : IRequest<bool>
{
    public Guid id { get; set; }
}

public class DeleteTodoCommandHandler
    : BaseCommandHandler<ITodoDbContext, cong_viec>, IRequestHandler<DeleteTodoCommand, bool>
{
    private readonly IIdentityService _identityService;

    public DeleteTodoCommandHandler(
        ITodoDbContext context,
        IMapper mapper,
        IMediator mediator,
        IConfiguration config,
        IIdentityService identityService)
        : base(context, mapper, mediator, config)
    {
        _identityService = identityService;
    }

    public async Task<bool> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var userId = _identityService.currentUser?.id
            ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập.");

        var entity = await _context.cong_viec
            .FirstOrDefaultAsync(x => x.id == request.id && x.nguoi_dung_id == userId, cancellationToken);

        if (entity == null)
            throw new RejectException(ErrorCode.NotFound, "Không tìm thấy công việc.");

        _context.cong_viec.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
```

### 4.6. `Todo.Application/Queries/GetTodoListQuery.cs`

```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.DTO;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;
using SharedKernel.Application.Queries;
using Todo.Application.DTO;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;
using Todo.Domain.Enums;

namespace Todo.Application.Queries;

public record GetTodoListQuery : IRequest<PagedResult<TodoDto>>
{
    public int? status { get; set; }
    public int page { get; set; } = 1;
    public int pageSize { get; set; } = 20;
}

public class GetTodoListQueryHandler
    : BaseQueryHandler<ITodoDbContext, cong_viec>, IRequestHandler<GetTodoListQuery, PagedResult<TodoDto>>
{
    private readonly IIdentityService _identityService;

    public GetTodoListQueryHandler(
        ITodoDbContext context,
        IMapper mapper,
        IConfiguration config,
        IIdentityService identityService)
        : base(context, mapper, config)
    {
        _identityService = identityService;
    }

    public async Task<PagedResult<TodoDto>> Handle(GetTodoListQuery request, CancellationToken cancellationToken)
    {
        var userId = _identityService.currentUser?.id
            ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập.");

        var query = _repo.Where(x => x.nguoi_dung_id == userId);

        if (request.status.HasValue)
        {
            var trangThai = (TrangThaiCongViec)request.status.Value;
            query = query.Where(x => x.trang_thai == trangThai);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.ngay_tao)
            .Skip((request.page - 1) * request.pageSize)
            .Take(request.pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TodoDto>
        {
            items = _mapper.Map<List<TodoDto>>(items),
            totalCount = totalCount,
            page = request.page,
            pageSize = request.pageSize
        };
    }
}
```

**Giải thích**: `_repo` là `IQueryable` đã `AsNoTracking` từ `BaseQueryHandler`. Đếm `CountAsync` **trước**
`Skip/Take` để `totalCount` là tổng toàn bộ, không phải số dòng của trang hiện tại.

### 4.7. `Todo.Application/Queries/GetTodoByIdQuery.cs`

```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;
using SharedKernel.Application.Queries;
using Todo.Application.DTO;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;

namespace Todo.Application.Queries;

public record GetTodoByIdQuery : IRequest<TodoDto>
{
    public Guid id { get; set; }
}

public class GetTodoByIdQueryHandler
    : BaseQueryHandler<ITodoDbContext, cong_viec>, IRequestHandler<GetTodoByIdQuery, TodoDto>
{
    private readonly IIdentityService _identityService;

    public GetTodoByIdQueryHandler(
        ITodoDbContext context,
        IMapper mapper,
        IConfiguration config,
        IIdentityService identityService)
        : base(context, mapper, config)
    {
        _identityService = identityService;
    }

    public async Task<TodoDto> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _identityService.currentUser?.id
            ?? throw new RejectException(ErrorCode.Unauthorized, "Không xác định được người dùng đăng nhập.");

        var entity = await _repo
            .FirstOrDefaultAsync(x => x.id == request.id && x.nguoi_dung_id == userId, cancellationToken);

        if (entity == null)
            throw new RejectException(ErrorCode.NotFound, "Không tìm thấy công việc.");

        return _mapper.Map<TodoDto>(entity);
    }
}
```

### 4.8. `Todo.Application/Validators/TodoValidators.cs`

```csharp
using FluentValidation;
using Todo.Application.Commands;
using Todo.Application.Queries;
using Todo.Domain.Enums;

namespace Todo.Application.Validators;

public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(x => x.title)
            .NotEmpty().WithMessage("Tiêu đề công việc không được để trống.")
            .MaximumLength(500).WithMessage("Tiêu đề công việc tối đa 500 ký tự.");
    }
}

public class UpdateTodoCommandValidator : AbstractValidator<UpdateTodoCommand>
{
    public UpdateTodoCommandValidator()
    {
        RuleFor(x => x.id)
            .NotEmpty().WithMessage("Id công việc không hợp lệ.");

        RuleFor(x => x.title)
            .NotEmpty().WithMessage("Tiêu đề công việc không được để trống.")
            .MaximumLength(500).WithMessage("Tiêu đề công việc tối đa 500 ký tự.");

        RuleFor(x => x.status)
            .Must(s => Enum.IsDefined(typeof(TrangThaiCongViec), s))
            .WithMessage("Trạng thái không hợp lệ (0 = chưa làm, 1 = đang làm, 2 = hoàn thành).");
    }
}

public class GetTodoListQueryValidator : AbstractValidator<GetTodoListQuery>
{
    public GetTodoListQueryValidator()
    {
        RuleFor(x => x.page)
            .GreaterThanOrEqualTo(1).WithMessage("page phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.pageSize)
            .InclusiveBetween(1, 100).WithMessage("pageSize phải nằm trong khoảng 1 đến 100.");

        RuleFor(x => x.status)
            .Must(s => !s.HasValue || Enum.IsDefined(typeof(TrangThaiCongViec), s.Value))
            .WithMessage("Trạng thái lọc không hợp lệ (chỉ nhận 0, 1 hoặc 2).");
    }
}
```

**Lưu ý dễ sai**: dùng `Enum.IsDefined(...)`, **không dùng `.IsInEnum()`** — vì property `status` khai kiểu
`int` (để client gửi số qua JSON), `IsInEnum()` chỉ hoạt động khi property là kiểu enum.

---

# Phase 5 — Todo.Infrastructure

### 5.1. `Todo.Infrastructure/Data/TodoDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SharedKernel.Infrastructure.Data;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;

namespace Todo.Infrastructure.Data;

public class TodoDbContext : BaseDbContext<TodoDbContext>, ITodoDbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options,
                         AuditableEntitySaveChangesInterceptor auditableInterceptor)
        : base(options, auditableInterceptor)
    {
    }

    public DbSet<cong_viec> cong_viec { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoDbContext).Assembly);
    }
}
```

**Giải thích**: kế thừa `BaseDbContext<T>` là tự động có interceptor điền `ngay_tao`/`ngay_chinh_sua` —
đây là lý do `createdAt`/`updatedAt` không phải viết code.

### 5.2. `Todo.Infrastructure/ConfigureServices.cs`

```csharp
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
```

**Giải thích**: copy đúng khuôn `Admin.Infrastructure/ConfigureServices.cs`, chỉ đổi DbContext + type marker
(`typeof(CreateTodoCommand)` thay cho `typeof(LoginCommand)`). Nhờ quét assembly nên sau này thêm
command/query/validator mới vào `Todo.Application` là tự chạy, khỏi sửa file này.

Lưu ý nhỏ: `AddSharedKernelInfrastructureServices()` đăng ký dư `IPasswordHasherService`/`IJwtTokenService`
(service Todo không cấp token, chỉ xác thực) — vô hại, và nó là chỗ đăng ký interceptor nên vẫn phải gọi.

---

# Phase 6 — Todo.Api (host riêng)

### 6.1. `Todo.Api/Program.cs` (ghi đè toàn bộ file mặc định)

```csharp
using SharedKernel.Api;
using SharedKernel.Api.Extensions;
using Todo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Tắt log cảnh báo thiếu license key của MediatR/AutoMapper (bản Community, không chặn chạy)
builder.Logging.AddFilter("LuckyPennySoftware", LogLevel.None);

builder.Services.AddSharedKernelApiServices();                          // IIdentityService, HttpContextAccessor
builder.Services.AddTodoInfrastructureServices(builder.Configuration);  // DbContext, MediatR, Validator, Mapper

builder.Services.AddControllers();
builder.Services.AddSharedKernelSwagger("Todo API (.NET 10)");
builder.Services.AddSharedKernelJwtAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

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

Gọn hơn hẳn `Admin.Api/Program.cs` vì phần JWT + Swagger đã nằm ở `HostConfigurationExtensions` (Phase 2.3).

### 6.2. `Todo.Api/Controllers/TodoController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api.Base;
using SharedKernel.Application.DTO;
using SharedKernel.Application.Exceptions;
using Todo.Application.Commands;
using Todo.Application.DTO;
using Todo.Application.Queries;

namespace Todo.Api.Controllers;

[Route("api/todo")]
[ApiController]
[Authorize]                       // áp cho cả 5 action, khỏi lặp từng cái
public class TodoController : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<TodoDto>> Create([FromBody] CreateTodoCommand request)
    {
        try
        {
            return Ok(await Mediator.Send(request));
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TodoDto>>> GetList([FromQuery] GetTodoListQuery request)
    {
        try
        {
            return Ok(await Mediator.Send(request));
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoDto>> GetById(Guid id)
    {
        try
        {
            return Ok(await Mediator.Send(new GetTodoByIdQuery { id = id }));
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TodoDto>> Update(Guid id, [FromBody] UpdateTodoCommand request)
    {
        try
        {
            request.id = id;    // id lấy từ route, không tin id trong body
            return Ok(await Mediator.Send(request));
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await Mediator.Send(new DeleteTodoCommand { id = id });
            return Ok(new { message = "Xóa công việc thành công." });
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }
}
```

### 6.3. `Todo.Api/appsettings.json` (ghi đè)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "CHÉP Y HỆT chuỗi kết nối trong Admin.Api/appsettings.json"
  },
  "JwtSettings": {
    "Secret": "PHẢI GIỐNG HỆT Admin.Api, sai 1 ký tự là token bị từ chối",
    "Issuer": "MyAuthServer",
    "Audience": "MyAuthClient"
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

**Giải thích**: `JwtSettings` ở đây **chỉ cần 3 khóa** `Secret`/`Issuer`/`Audience` để *xác thực* token.
Không cần `AccessTokenExpiryMinutes`, `RefreshTokenExpiryDays`, `MaxFailedAttempts`, `LockoutMinutes` —
đó là việc *cấp* token, thuộc trách nhiệm của Admin.Api.

### 6.4. `Todo.Api/Properties/launchSettings.json`

Đổi `applicationUrl` sang port riêng để không đụng Admin.Api (`:5043`):

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5100",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    }
  }
}
```

### Bảng endpoint

| Method | Route (`:5100`) | Body / Query | Kết quả |
|---|---|---|---|
| POST | `/api/todo` | `{ "title": "Mua sữa" }` | `200` + TodoDto |
| GET | `/api/todo?status=0&page=1&pageSize=20` | query | `200` + PagedResult |
| GET | `/api/todo/{id}` | — | `200` / `404` |
| PUT | `/api/todo/{id}` | `{ "title": "...", "status": 2 }` | `200` / `404` |
| DELETE | `/api/todo/{id}` | — | `200` / `404` |

---

# Phase 7 — Build & kiểm thử

### 7.1. Build

```powershell
# QUAN TRỌNG: tắt mọi terminal đang chạy dotnet run trước (Ctrl+C),
# nếu không sẽ dính MSB3027/MSB3021 khóa file DLL — không phải lỗi code.
dotnet build          # kỳ vọng: 0 Warning, 0 Error
```

Nếu thấy `warning NU1608` nhắc tới Pomelo/EntityFrameworkCore.Relational → bạn đã lỡ cài EF Core 10 cho
`Todo.*`; sửa version trong `Todo.Infrastructure.csproj` về `9.0.19` rồi build lại.

### 7.2. Kiểm tra Solution Folder hiển thị đúng

Mở `api-net10.slnx` bằng Visual Studio / Rider → Solution Explorer phải hiện đúng **3 nhóm**
`SharedKernel`, `Admin`, `Todo`, mỗi nhóm 4 project. Nếu thấy project nào còn nằm lơ lửng ngoài nhóm
→ dòng `<Project>` đó chưa được đưa vào trong thẻ `<Folder>` tương ứng (xem Phase 0).

Kiểm tra bằng CLI cũng được — phải liệt kê đủ 12 project:
```powershell
dotnet sln list
```

### 7.3. Chạy 2 service (2 terminal riêng)

```powershell
# Terminal 1
dotnet run --project Admin.Api      # :5043

# Terminal 2
dotnet run --project Todo.Api       # :5100
```

### 7.4. Test tay

1. Mở `http://localhost:5043/swagger` → `POST /api/auth/login` với `admin` / `12345678aA@` → copy `accessToken`.
2. Mở `http://localhost:5100/swagger` → bấm **Authorize**, dán token (không kèm chữ "Bearer ").
   → **Đây là phép thử quan trọng nhất**: token do service A cấp mà service B chấp nhận, chứng minh
   phần chia tách JWT hoạt động.
3. `POST /api/todo` với `{ "title": "Mua sữa" }` → `200`, `status: 0`, `completedAt: null`,
   **`createdAt` có giá trị** (bằng chứng interceptor chạy đúng).
4. `GET /api/todo` → `items` có 1 phần tử, `totalCount: 1`, `totalPages: 1`.
5. `PUT /api/todo/{id}` với `{ "title": "Mua sữa tươi", "status": 2 }` → **`completedAt` tự có giá trị**.
6. `PUT` lại với `"status": 0` → **`completedAt` quay về `null`**.
7. `DELETE /api/todo/{id}` → `200`; gọi lại `GET /api/todo/{id}` → **`404`**.

### 7.5. Các case lỗi bắt buộc thử

| Thử | Kỳ vọng |
|---|---|
| Bấm **Authorize → Logout** rồi gọi `GET /api/todo` | `401` |
| Gọi `:5100/api/todo` với token bị sửa 1 ký tự | `401` |
| `POST /api/todo` với `{ "title": "" }` | `400` "Tiêu đề công việc không được để trống." |
| `GET /api/todo/{guid ngẫu nhiên}` | `404` |
| `GET /api/todo?pageSize=0` | `400` "pageSize phải nằm trong khoảng 1 đến 100." |
| `PUT /api/todo/{id}` với `"status": 9` | `400` "Trạng thái không hợp lệ..." |

### 7.6. Không được làm hỏng service Admin

```powershell
powershell -File scripts/Test-AuthFlow.ps1      # vẫn phải 19 PASS / 0 FAIL
```

---

## Bảng tra lỗi thường gặp

| Triệu chứng | Nguyên nhân |
|---|---|
| `MissingMethodException: ...AbstractionsStrings.ArgumentIsEmpty...` | `Todo.Infrastructure` cài nhầm EF Core 10 — phải ghim `9.0.19` cho khớp Pomelo 9.0.0 |
| `MSB3027` / `MSB3021` khi build | Đang có `dotnet run` chạy khóa DLL — tắt cả 2 terminal rồi build lại |
| Gọi `:5100` luôn trả `401` dù token mới lấy | `JwtSettings:Secret`/`Issuer`/`Audience` ở 2 appsettings.json không khớp nhau |
| `500` kèm "Unknown column 'x.ngay_tao'" | Chưa chạy `Database/03_cong_viec.sql`, hoặc bảng thiếu 4 cột audit |
| `createdAt` trả `null` | Gọi `_mapper.Map` **trước** `SaveChangesAsync` — phải map sau |
| `CS0535 ... does not implement 'cong_viec'` | Thêm `DbSet` vào `ITodoDbContext` mà quên thêm vào `TodoDbContext` |
| Validator không chạy | Quên `cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>))` hoặc `AddValidatorsFromAssembly` trong `Todo.Infrastructure/ConfigureServices.cs` |
| Todo của user khác vẫn xem được | Thiếu điều kiện `x.nguoi_dung_id == userId` trong truy vấn |
| `GET /api/todo` trả `200` dù không token | Thiếu `[Authorize]` trên class `TodoController` |
| Port 5100 bị chiếm | Đổi `applicationUrl` trong `Todo.Api/Properties/launchSettings.json` |

---

## Việc tùy chọn làm sau (khi Todo đã chạy ổn)

- **Rút gọn `Admin.Api/Program.cs`**: thay ~60 dòng cấu hình JWT + Swagger inline bằng 2 lời gọi
  `AddSharedKernelJwtAuthentication(...)` + `AddSharedKernelSwagger("Auth API (.NET 10)")`. Chỉ nên làm
  **sau khi** cả 2 service đã test xanh, và chạy lại `Test-AuthFlow.ps1` để chắc không hỏng gì.
- **`scripts/Test-TodoCrud.ps1`**: test tự động 5 endpoint (khuôn `Test-AuthFlow.ps1`, login ở `:5043`
  rồi gọi `:5100`). Nếu tự viết, nhớ lưu file với **UTF-8 BOM**, không thì Windows PowerShell 5.1
  parse lỗi ký tự tiếng Việt.
- **Cập nhật `CLAUDE.md`**: thêm sơ đồ 3 nhóm (SharedKernel / Admin / Todo), port của từng service,
  ràng buộc `JwtSettings:Secret` phải khớp giữa các service, và quy trình chuẩn để dựng service nghiệp vụ
  tiếp theo (dùng chính tài liệu này làm khuôn).
- **Dọn todo mồ côi** khi user bị xóa (do không có FK): thêm job định kỳ hoặc domain event từ Admin.
- **Nếu sau này muốn gom thư mục vật lý thật** (`SharedKernel/`, `Admin/`, `Todo/` trên đĩa): lúc đó phải
  sửa đường dẫn `ProjectReference` trong cả 12 `.csproj`, sửa `Path` trong `.slnx`, sửa lệnh
  `dotnet run --project ...` và link trong `CLAUDE.md`. Làm được nhưng nên tách thành 1 lần refactor riêng,
  có chạy lại `Test-AuthFlow.ps1` + `Test-TodoCrud.ps1` để verify — đừng trộn chung với việc thêm feature.
