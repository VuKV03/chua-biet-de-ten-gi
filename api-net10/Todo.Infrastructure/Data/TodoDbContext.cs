using Microsoft.EntityFrameworkCore;
using SharedKernel.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Todo.Application.Interfaces;
using Todo.Domain.Entities;

namespace Todo.Infrastructure.Data
{
    public class TodoDbContext : BaseDbContext<TodoDbContext>, ITodoDbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options, 
            AuditableEntitySaveChangesInterceptor auditableInterceptor)
            : base(options, auditableInterceptor)
        { 
        }

        public DbSet<cong_viec> cong_viec { get; set; } = null;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoDbContext).Assembly);
        }
    }
}
