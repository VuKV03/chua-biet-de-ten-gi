# CLAUDE.md

Tài liệu ngữ cảnh cho Claude Code khi làm việc trong repo này. Đọc file này trước, sau đó xem
[implementation_plan.md](implementation_plan.md) để hiểu thiết kế gốc — nhưng lưu ý phần
**"Khác biệt so với implementation_plan.md"** bên dưới, vì một số đoạn code trong plan gốc
**không build được** với version package thực tế đã cài và đã được sửa trực tiếp trong source.

## Tổng quan dự án

API Auth chuẩn doanh nghiệp trên .NET 10, Clean Architecture + CQRS (MediatR), MySQL (Pomelo),
JWT access/refresh token có rotation + phát hiện replay attack. Toàn bộ nghiệp vụ, tên bảng/cột
đặt theo tiếng Việt không dấu (`tai_khoan`, `mat_khau`, `ngay_tao`...) — đây là chủ đích kế thừa
từ dự án tham chiếu `RJS_api_net6`, không phải lỗi quy ước đặt tên.

8 project theo Clean Architecture 2 tầng (SharedKernel dùng chung + Admin module nghiệp vụ):

```
SharedKernel.Domain          -> SharedKernel.Application -> SharedKernel.Infrastructure -> SharedKernel.Api
Admin.Domain (+SharedKernel.Domain) -> Admin.Application -> Admin.Infrastructure -> Admin.Api
```

## Build / Run

```powershell
dotnet build                 # từ thư mục gốc — phải ra "Build succeeded, 0 Error"
cd Admin.Api
dotnet run                   # mặc định http://localhost:5043 (xem Properties/launchSettings.json)
```

Swagger UI: `http://localhost:5043/swagger`. File test nhanh: [Admin.Api/Admin.Api.http](Admin.Api/Admin.Api.http)
(đăng nhập → copy accessToken/refreshToken → thay vào các request còn lại).

## Test toàn bộ API tự động

[scripts/Test-AuthFlow.ps1](scripts/Test-AuthFlow.ps1) — test end-to-end cả 4 endpoint qua HTTP thật
(không cần cài thêm gì, chạy thẳng bằng PowerShell có sẵn trên Windows), gồm cả các case bảo mật
(không chỉ happy path): validation input rỗng, sai mật khẩu/tài khoản, token rotation khi refresh,
phát hiện & xử lý replay attack (dùng lại refresh token cũ), revoke token khi logout, truy cập thiếu/sai
token. Chạy sau khi đã `dotnet run` và đã seed DB:

```powershell
powershell -File scripts/Test-AuthFlow.ps1
```

In ra từng dòng `[PASS]`/`[FAIL]` và tổng kết cuối cùng `N PASS / N FAIL` (exit code 0 nếu toàn bộ pass).
Xem docstring đầu file để biết tham số (`-BaseUrl`, `-TaiKhoan`, `-MatKhau`, `-TestLockout`).

## Cấu hình cần thiết trước khi chạy được thật (chưa làm — cần bạn tự cấu hình)

1. Chạy 2 script SQL theo đúng thứ tự trên MySQL 8.0+/MariaDB thật của bạn:
   [Database/01_schema.sql](Database/01_schema.sql) rồi [Database/02_seed_admin.sql](Database/02_seed_admin.sql).
2. Sửa `ConnectionStrings:DefaultConnection` trong [Admin.Api/appsettings.json](Admin.Api/appsettings.json)
   — hiện đang là placeholder `User=root;Password=your_password`. Máy dev hiện có MySQL chạy ở
   `localhost:3306` nhưng từ chối cặp user/pass mặc định này (đã xác nhận qua smoke test thực tế).
3. Tài khoản seed: `admin` / `12345678aA@` — hash trong `02_seed_admin.sql` được sinh **thật** bằng
   đúng thuật toán PBKDF2-HMACSHA512 của `PasswordHasherService`, không phải hash mẫu chép tay.
4. `JwtSettings:Secret` trong appsettings.json là secret demo hard-code — trước khi lên production
   phải chuyển sang User Secrets / biến môi trường / Key Vault, không commit secret thật vào repo.

## Khác biệt so với implementation_plan.md (đã sửa để build & chạy không lỗi)

Plan gốc là bản thiết kế tham khảo; khi triển khai thật với version package đã `dotnet add package`
sẵn trong Phase 0 (MediatR 14.2.0, AutoMapper 16.2.0, Swashbuckle.AspNetCore 10.2.3 → kéo theo
Microsoft.OpenApi 2.7.5...), một số đoạn code trong plan **không compile được** hoặc gây bug runtime.
Toàn bộ đã được sửa trực tiếp trong source, xác nhận bằng `dotnet build` (0 Error) + `dotnet run`
smoke test thực tế (Swagger 200, login rỗng → 400 validation đúng, login sai credential DB → 500
có message rõ ràng chứ không crash app). Danh sách khác biệt:

| # | Vấn đề trong plan gốc | File thực tế | Đã sửa thế nào |
|---|---|---|---|
| 1 | `JwtTokenService` (ở Infrastructure) dùng `JwtSecurityTokenHandler`/`SymmetricSecurityKey` nhưng plan chỉ gán package `Microsoft.AspNetCore.Authentication.JwtBearer` cho tầng **Api**, không có ở Infrastructure → không compile | [SharedKernel.Infrastructure.csproj](SharedKernel.Infrastructure/SharedKernel.Infrastructure.csproj) | Thêm package `System.IdentityModel.Tokens.Jwt` trực tiếp vào Infrastructure |
| 2 | `IIdentityService` dùng type `HttpContext` nhưng project là classlib (`Sdk.NET`), không tự có ASP.NET Core shared framework → không compile | [SharedKernel.Application.csproj](SharedKernel.Application/SharedKernel.Application.csproj) | Thêm `<FrameworkReference Include="Microsoft.AspNetCore.App"/>` |
| 3 | `AppDomain.CurrentDomain.Load("Admin.Application")` — load assembly bằng chuỗi tên, dễ vỡ khi refactor/rename, phụ thuộc assembly đã được load sẵn vào AppDomain | [Admin.Infrastructure/ConfigureServices.cs](Admin.Infrastructure/ConfigureServices.cs) | Thay bằng `typeof(LoginCommand).Assembly` — tham chiếu type thật |
| 4 | Đã cài `FluentValidation.DependencyInjectionExtensions` ở Phase 0 nhưng **không có bất kỳ validator hay pipeline behavior nào** trong toàn bộ plan → cài xong không dùng, validate input rỗng vẫn lọt xuống DB | [SharedKernel.Application/Behaviours/ValidationBehaviour.cs](SharedKernel.Application/Behaviours/ValidationBehaviour.cs), [Admin.Application/Auth/Validators/](Admin.Application/Auth/Validators/) | Thêm MediatR pipeline behavior chạy `IValidator<T>` trước handler + validator cho Login/RefreshToken, đăng ký qua `AddValidatorsFromAssembly` + `cfg.AddOpenBehavior(...)` trong `Admin.Infrastructure/ConfigureServices.cs` |
| 5 | Swashbuckle.AspNetCore 10.x kéo theo **Microsoft.OpenApi 2.x**, đổi breaking: namespace `Microsoft.OpenApi.Models` gộp thẳng vào `Microsoft.OpenApi`; `OpenApiSecurityScheme.Reference` **không còn tồn tại** — code Swagger Bearer auth trong plan không compile | [Admin.Api/Program.cs](Admin.Api/Program.cs) | Viết lại theo API mới chính thức của Swashbuckle: `AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] })`, `SecuritySchemeType.Http` + `BearerFormat="JWT"` thay vì `ApiKey` |
| 6 | `services.AddAutoMapper(appAssembly)` không khớp overload còn lại của AutoMapper 16.2.0 (chỉ còn overload nhận `Action<IMapperConfigurationExpression>` làm tham số đầu) → không compile | [Admin.Infrastructure/ConfigureServices.cs](Admin.Infrastructure/ConfigureServices.cs) | Đổi thành `services.AddAutoMapper(cfg => { }, appAssembly)` |
| 7 | `ErrorCtr.ExtractErrorInfo` luôn trả `400 BadRequest` cho mọi `ValidationException`, bỏ qua enum `ErrorCode.NotFound`/`ErrorCode.Unauthorized` đã định nghĩa sẵn — enum có mà không dùng | [SharedKernel.Application/Exceptions/ErrorCtr.cs](SharedKernel.Application/Exceptions/ErrorCtr.cs) | `RejectException` giữ lại `ErrorCode`, `ExtractErrorInfo` map đúng: `NotFound`→404, `Unauthorized`→401, còn lại→400 |
| 8 | `JwtTokenService.GetPrincipalFromExpiredToken` gọi `handler.ValidateToken(...)` **throw exception** khi token hỏng/hết hạn sai định dạng thay vì trả `null` như hợp đồng interface — khiến `RefreshTokenCommandHandler` (đang check `if (principal == null)`) không bao giờ chạm nhánh lỗi 400 dự kiến, mà văng thẳng lên thành lỗi 500 không kiểm soát | [SharedKernel.Infrastructure/Services/JwtTokenService.cs](SharedKernel.Infrastructure/Services/JwtTokenService.cs) | Bọc `try/catch (SecurityTokenException / ArgumentException)` trả `null`, đúng hợp đồng nullable của interface |
| 9 | Seed SQL trong plan dùng hash mật khẩu mẫu copy dạng **ASP.NET Core Identity** (`AQAAAAIAAYag...`) — sai định dạng hoàn toàn so với `PasswordHasherService` tự viết (marker byte `0x01` + salt 16 byte + subkey PBKDF2-HMACSHA512 32 byte) → login bằng tài khoản seed **luôn thất bại** dù đúng mật khẩu | [Database/02_seed_admin.sql](Database/02_seed_admin.sql) | Sinh lại hash **thật** bằng cách chạy đúng thuật toán của `PasswordHasherService.HashPassword("12345678aA@")` trong một console app tạm rồi nhúng kết quả vào SQL |
| 10 | **Bug runtime nghiêm trọng, chỉ lộ ra khi có DB thật**: Phase 0 pin `Microsoft.EntityFrameworkCore*` = `10.0.11` nhưng `Pomelo.EntityFrameworkCore.MySql` mới nhất trên NuGet chỉ có bản `9.0.0` (constraint `Microsoft.EntityFrameworkCore.Relational` `9.0.0–9.0.999`) — **chưa hề có bản Pomelo nào hỗ trợ EF Core 10**. Build vẫn "thành công" (NuGet chỉ cảnh báo NU1608) nhưng gọi API chạm DB thì vỡ ở tầng binary nội bộ: `POST /api/auth/login` → 500 `MissingMethodException: Method not found: 'System.String Microsoft.EntityFrameworkCore.Diagnostics.AbstractionsStrings.ArgumentIsEmpty(System.Object)'` | `SharedKernel.Application.csproj`, `SharedKernel.Infrastructure.csproj`, `Admin.Infrastructure.csproj` | Hạ toàn bộ `Microsoft.EntityFrameworkCore` / `Microsoft.EntityFrameworkCore.Design` xuống `9.0.19` (bản 9.x mới nhất, đúng khoảng Pomelo yêu cầu), giữ nguyên `Pomelo.EntityFrameworkCore.MySql = 9.0.0`. Đã xác nhận bằng `dotnet build` (0 Warning/0 Error, hết cả NU1608) + chạy full `scripts/Test-AuthFlow.ps1` với DB thật: **19/19 PASS** |

Ngoài ra, 2 file `.sql` ở Phase 1 trước đây **chỉ tồn tại dưới dạng đoạn code trong markdown**, chưa
có file thật trong repo → đã tách thành [Database/01_schema.sql](Database/01_schema.sql) và
[Database/02_seed_admin.sql](Database/02_seed_admin.sql).

## Cảnh báo đã biết (không phải lỗi, không cần sửa)

- LuckyPennySoftware (chủ sở hữu mới của MediatR/AutoMapper từ v13+/v15+) in log cảnh báo thiếu
  license key ở bản Community — **không chặn chạy ứng dụng**, chỉ log. Đã tắt qua
  `builder.Logging.AddFilter("LuckyPennySoftware", LogLevel.None)` trong `Program.cs`. Muốn gắn
  license key thật thì đặt biến môi trường `MEDIATR_LICENSE_KEY` / `AUTOMAPPER_LICENSE_KEY`.
- Khi rebuild lúc đang có `dotnet run` chạy sẵn ở terminal khác, `dotnet build`/`dotnet run` sẽ báo
  lỗi `MSB3027`/`MSB3021` (không copy được DLL vì bị khóa) — không phải lỗi code, chỉ cần dừng tiến
  trình đang chạy (Ctrl+C ở terminal đó, hoặc `Stop-Process`) rồi build lại.

## Trạng thái hiện tại

- Phase 0 → 8 của implementation_plan.md: **đã triển khai đầy đủ**, `dotnet build` sạch hoàn toàn
  — **0 Warning, 0 Error** (kể cả NU1608 cũng đã hết sau khi hạ EF Core xuống 9.0.19, xem mục #10
  ở bảng trên).
- Đã test end-to-end thật với MySQL thật (không phải smoke test suông): chạy
  `scripts/Test-AuthFlow.ps1` → **19/19 PASS** — cả 4 API (login/refresh-token/me/logout) lẫn 2 cơ
  chế bảo mật quan trọng (token rotation khi refresh, phát hiện & revoke khi replay refresh token
  cũ) đều hoạt động đúng.
