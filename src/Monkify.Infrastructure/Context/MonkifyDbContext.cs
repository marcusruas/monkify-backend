using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Monkify.Common.Models;
using Monkify.Domain.Entities;
using System.Data;

namespace Monkify.Infrastructure.Context
{
    public class MonkifyDbContext : DbContext
    {
        public DbSet<SessionType> SessionTypes { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SessionType>(builder =>
            {
                builder.HasKey(x => x.Id);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<TableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.Now;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
