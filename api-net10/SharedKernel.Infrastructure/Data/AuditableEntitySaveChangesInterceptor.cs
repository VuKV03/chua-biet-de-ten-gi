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
