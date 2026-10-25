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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
        });





    }
}