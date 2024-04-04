using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Monkify.Common.Models;
using System.Data;
using Monkify.Domain.Users.Entities;
using Monkify.Domain.Sessions.Entities;

namespace Monkify.Infrastructure.Context
{
    public class MonkifyDbContext : DbContext
    {
        public MonkifyDbContext(DbContextOptions<MonkifyDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<SessionParameters> SessionParameters { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Bet> SessionBets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Username).IsRequired().HasMaxLength(50);
                builder.Property(x => x.Email).IsRequired().HasMaxLength(254);
                builder.Property(x => x.Active).IsRequired().HasDefaultValue(false);
                builder.Property(x => x.WalletId).IsRequired().HasMaxLength(40);
            });

            modelBuilder.Entity<SessionParameters>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.MinimumNumberOfPlayers).IsRequired().HasDefaultValue(1);
                builder.Property(x => x.ChoiceRequiredLength).IsRequired().HasDefaultValue(1);
                builder.Property(x => x.Active).IsRequired().HasDefaultValue(false);
            });

            modelBuilder.Entity<Session>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.ParametersId).IsRequired();
                builder.HasOne(x => x.Parameters).WithMany().HasForeignKey(x => x.ParametersId);
                builder.HasMany(x => x.Bets).WithOne(x => x.Session).HasForeignKey(x => x.SessionId);
            });

            modelBuilder.Entity<Bet>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.BetChoice).IsRequired().HasMaxLength(20);
                builder.Property(x => x.BetAmount).HasPrecision(8, 8).IsRequired();
                builder.Property(x => x.Won).IsRequired().HasDefaultValue(false);
                builder.HasOne(x => x.User).WithMany(x => x.Bets).HasForeignKey(x => x.UserId);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<TableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.UtcNow;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
