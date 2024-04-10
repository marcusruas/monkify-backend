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
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Infrastructure.Context
{
    public class MonkifyDbContext : DbContext
    {
        public MonkifyDbContext(DbContextOptions<MonkifyDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<SessionParameters> SessionParameters { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionLog> SessionLogs { get; set; }
        public DbSet<Bet> SessionBets { get; set; }
        public DbSet<BetTransactionLog> BetLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Username).IsRequired().HasMaxLength(50);
                builder.Property(x => x.Email).IsRequired().HasMaxLength(254);
                builder.Property(x => x.Active).IsRequired().HasDefaultValue(false);
                builder.Property(x => x.Wallet).IsRequired().HasMaxLength(40);
            });

            modelBuilder.Entity<SessionParameters>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RequiredAmount).HasPrecision(8, 8).IsRequired();
                builder.Property(x => x.MinimumNumberOfPlayers).IsRequired().HasDefaultValue(1);
                builder.Property(x => x.ChoiceRequiredLength).IsRequired().HasDefaultValue(1);
                builder.Property(x => x.AcceptDuplicatedCharacters).IsRequired().HasDefaultValue(true);
                builder.Property(x => x.Active).IsRequired().HasDefaultValue(false);
            });

            modelBuilder.Entity<Session>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Status).IsRequired().HasDefaultValue(SessionStatus.WaitingBets);
                builder.Property(x => x.ParametersId).IsRequired();
                builder.HasOne(x => x.Parameters).WithMany().HasForeignKey(x => x.ParametersId);
                builder.HasMany(x => x.Bets).WithOne(x => x.Session).HasForeignKey(x => x.SessionId);
                builder.HasMany(x => x.Logs).WithOne(x => x.Session).HasForeignKey(x => x.SessionId);
            });

            modelBuilder.Entity<SessionLog>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.NewStatus).IsRequired().HasDefaultValue(SessionStatus.WaitingBets);
            });

            modelBuilder.Entity<Bet>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Choice).IsRequired().HasMaxLength(20);
                builder.Property(x => x.Amount).HasPrecision(8, 8).IsRequired();
                builder.Property(x => x.Won).IsRequired().HasDefaultValue(false);
                builder.HasMany(x => x.Logs).WithOne(x => x.Bet).HasForeignKey(x => x.BetId);
                builder.HasOne(x => x.User).WithMany(x => x.Bets).HasForeignKey(x => x.UserId);
            });

            modelBuilder.Entity<BetTransactionLog>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Amount).HasPrecision(8, 8).IsRequired();
                builder.Property(x => x.Wallet).IsRequired().HasMaxLength(40);
                builder.Property(x => x.Signature).IsRequired().HasMaxLength(100);
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
