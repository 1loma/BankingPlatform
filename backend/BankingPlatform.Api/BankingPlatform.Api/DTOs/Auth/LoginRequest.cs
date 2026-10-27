using System.ComponentModel.DataAnnotations;

namespace BankingPlatform.Api.DTOs.Auth;

public class LoginRequest
{
    // Email entered by the user
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Plain password received
    [Required]
    public string Password { get; set; } = string.Empty;
}