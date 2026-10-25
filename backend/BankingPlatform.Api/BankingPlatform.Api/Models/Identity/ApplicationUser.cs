using BankingPlatform.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace BankingPlatform.Api.Models.Identity;

public class ApplicationUser : IdentityUser
{
    // When the user registered
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Whether the user account is enabled
    public bool IsActive { get; set; } = true;

    // Navigation: User -> Customer (one-to-one)
    public Customer? Customer { get; set; }
}