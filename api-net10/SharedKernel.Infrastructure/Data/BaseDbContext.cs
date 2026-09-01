using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Interfaces;

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
