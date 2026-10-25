using BankingPlatform.Api.Models.Identity;
namespace BankingPlatform.Api.Models;

public class Customer
{
    // Primary key
    public int Id { get; set; }

    // Foreign key referencing AspNetUsers
    public string UserId { get; set; } = string.Empty;

    // Personal information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }

    // Profile creation date
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation: Customer -> User
    public ApplicationUser User { get; set; } = null!;
}