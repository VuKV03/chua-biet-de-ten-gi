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