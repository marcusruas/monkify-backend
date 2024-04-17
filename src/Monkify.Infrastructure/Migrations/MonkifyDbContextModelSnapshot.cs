﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Monkify.Infrastructure.Context;

#nullable disable

namespace Monkify.Infrastructure.Migrations
{
    [DbContext(typeof(MonkifyDbContext))]
    partial class MonkifyDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.Bet", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("Amount")
                        .HasPrecision(18, 9)
                        .HasColumnType("decimal(18,9)");

                    b.Property<string>("Choice")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<bool>("Refunded")
                        .HasColumnType("bit");

                    b.Property<Guid>("SessionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Wallet")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<bool>("Won")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.HasKey("Id");

                    b.HasIndex("SessionId");

                    b.ToTable("SessionBets");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.BetTransactionLog", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("Amount")
                        .HasPrecision(18, 9)
                        .HasColumnType("decimal(18,9)");

                    b.Property<Guid>("BetId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Signature")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime?>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("BetId");

                    b.ToTable("TransactionLogs");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.PresetChoice", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Choice")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ParametersId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ParametersId");

                    b.ToTable("PresetChoices");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.Session", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ParametersId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Status")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<DateTime?>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("WinningChoice")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.HasKey("Id");

                    b.HasIndex("ParametersId");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.SessionLog", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("NewStatus")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int?>("PreviousStatus")
                        .HasColumnType("int");

                    b.Property<Guid>("SessionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SessionId");

                    b.ToTable("SessionLogs");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.SessionParameters", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("AcceptDuplicatedCharacters")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<bool>("Active")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<int?>("ChoiceRequiredLength")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(1);

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("MinimumNumberOfPlayers")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(1);

                    b.Property<decimal>("RequiredAmount")
                        .HasPrecision(18, 9)
                        .HasColumnType("decimal(18,9)");

                    b.Property<int>("SessionCharacterType")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("SessionParameters");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.Bet", b =>
                {
                    b.HasOne("Monkify.Domain.Sessions.Entities.Session", "Session")
                        .WithMany("Bets")
                        .HasForeignKey("SessionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Session");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.BetTransactionLog", b =>
                {
                    b.HasOne("Monkify.Domain.Sessions.Entities.Bet", "Bet")
                        .WithMany("Logs")
                        .HasForeignKey("BetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Bet");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.PresetChoice", b =>
                {
                    b.HasOne("Monkify.Domain.Sessions.Entities.SessionParameters", "Parameters")
                        .WithMany("PresetChoices")
                        .HasForeignKey("ParametersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Parameters");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.Session", b =>
                {
                    b.HasOne("Monkify.Domain.Sessions.Entities.SessionParameters", "Parameters")
                        .WithMany()
                        .HasForeignKey("ParametersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Parameters");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.SessionLog", b =>
                {
                    b.HasOne("Monkify.Domain.Sessions.Entities.Session", "Session")
                        .WithMany("Logs")
                        .HasForeignKey("SessionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Session");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.Bet", b =>
                {
                    b.Navigation("Logs");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.Session", b =>
                {
                    b.Navigation("Bets");

                    b.Navigation("Logs");
                });

            modelBuilder.Entity("Monkify.Domain.Sessions.Entities.SessionParameters", b =>
                {
                    b.Navigation("PresetChoices");
                });
#pragma warning restore 612, 618
        }
    }
}
