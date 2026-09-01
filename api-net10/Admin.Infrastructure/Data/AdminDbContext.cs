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
