using System.Text;
using Admin.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SharedKernel.Api;

var builder = WebApplication.CreateBuilder(args);

// 0. LuckyPennySoftware (MediatR/AutoMapper) chỉ ghi log cảnh báo khi chưa cấu hình license key ở bản Community,
//    không chặn ứng dụng chạy. Để tắt cảnh báo hoặc gắn license key hợp lệ, đặt biến môi trường
//    MEDIATR_LICENSE_KEY / AUTOMAPPER_LICENSE_KEY (xem https://luckypennysoftware.com/faq).
builder.Logging.AddFilter("LuckyPennySoftware", LogLevel.None);

// 1. Đăng ký Services các tầng
builder.Services.AddSharedKernelApiServices();
builder.Services.AddAdminInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Swagger với Authorization Bearer
// Lưu ý: Microsoft.OpenApi 2.x (kéo theo bởi Swashbuckle.AspNetCore 10.x trên .NET 10) đổi cách khai báo
// security requirement: không còn OpenApiSecurityScheme.Reference, thay bằng OpenApiSecuritySchemeReference
// nhận vào OpenApiDocument đang generate (tham khảo docs/configure-and-customize-swaggergen.md của Swashbuckle).
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API (.NET 10)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Dán riêng accessToken vào đây (KHÔNG kèm chữ 'Bearer ' phía trước — Swagger UI tự thêm).",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
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
