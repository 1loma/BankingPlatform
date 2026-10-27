using System.ComponentModel.DataAnnotations;

namespace BankingPlatform.Api.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}