using Microsoft.EntityFrameworkCore;
using Monkify.Common.Models;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Monkify.Infrastructure.Context
{
    public class MonkifyDbContext : DbContext
    {
        public MonkifyDbContext(DbContextOptions<MonkifyDbContext> options) : base(options) { }

        public DbSet<SessionParameters> SessionParameters { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionLog> SessionLogs { get; set; }
        public DbSet<Bet> SessionBets { get; set; }
        public DbSet<BetTransactionLog> BetLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SessionParameters>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RequiredAmount).HasPrecision(18, 9).IsRequired();
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
                builder.Property(x => x.Wallet).IsRequired().HasMaxLength(50);
                builder.Property(x => x.Amount).HasPrecision(18, 9).IsRequired();
                builder.Property(x => x.Won).IsRequired().HasDefaultValue(false);
                builder.HasMany(x => x.Logs).WithOne(x => x.Bet).HasForeignKey(x => x.BetId);
            });

            modelBuilder.Entity<BetTransactionLog>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Amount).HasPrecision(18, 9).IsRequired();
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

            var result = await base.SaveChangesAsync(cancellationToken);
            ChangeTracker.Clear();
            return result;
        }
    }
}
