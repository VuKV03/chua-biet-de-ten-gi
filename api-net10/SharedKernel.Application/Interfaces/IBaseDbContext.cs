using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Application.Interfaces;

public interface IBaseDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
