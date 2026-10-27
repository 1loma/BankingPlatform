using BankingPlatform.Api.Models;
using BankingPlatform.Api.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Api.Data;

public class BankingDbContext : IdentityDbContext<ApplicationUser>
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccCus> AccountCustomers => Set<AccCus>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.Address)
                .HasMaxLength(300);

            entity.HasIndex(c => c.UserId)
                .IsUnique();
            // Each user can have one customer profile
            entity.HasOne(c => c.User)
                .WithOne(u => u.Customer)
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);


        // ACCOUNTS
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");

            entity.HasKey(a => a.Id);

            entity.HasIndex(a => a.AccountNumber).IsUnique();

            entity.Property(a => a.AccountNumber)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(a => a.Currency)
                .HasColumnType("char(3)")
                .IsRequired();

            // Save enum names instead of numeric values
            entity.Property(a => a.AccountType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // SQL Server timestamp for concurrency detection
            entity.Property(a => a.RowVersion)
                .IsRowVersion();

            entity.Property(a => a.OpenedAtUtc)
                .HasColumnType("datetimeoffset");

            entity.Property(a => a.CreatedAtUtc)
                .HasColumnType("datetimeoffset");

            entity.Property(a => a.ClosedAtUtc)
                .HasColumnType("datetimeoffset");
        });


        // ACCOUNT-CUSTOMER OWNERSHIP
        modelBuilder.Entity<AccCus>(entity =>
        {
            entity.ToTable("ACC_CUS");

            // Composite primary key
            entity.HasKey(ac => new
            {
                ac.AccountId,
                ac.CustomerId
            });

            entity.Property(ac => ac.Permission)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(ac => ac.AddedAtUtc)
                .HasColumnType("datetimeoffset");

            // Each junction row references one Account
            entity.HasOne(ac => ac.Account)
                .WithMany(a => a.AccountCustomers)
                .HasForeignKey(ac => ac.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Each junction row references one Customer
            entity.HasOne(ac => ac.Customer)
                .WithMany(c => c.AccountCustomers)
                .HasForeignKey(ac => ac.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    });





    }
}