using Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Interfaces;

namespace Admin.Application.Interfaces;

public interface IAdminDbContext : IBaseDbContext
{
    DbSet<nguoi_dung> nguoi_dung { get; set; }
    DbSet<nguoi_dung_refresh_token> nguoi_dung_refresh_token { get; set; }
}
