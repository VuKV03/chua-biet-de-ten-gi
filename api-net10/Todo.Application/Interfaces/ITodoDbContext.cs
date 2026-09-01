using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Interfaces;
using Todo.Domain.Entities;
using SharedKernel.Application.Interfaces;

namespace Todo.Application.Interfaces
{
    public interface ITodoDbContext : IBaseDbContext
    {
        DbSet<cong_viec> cong_viec { get; set; }
    }
}
