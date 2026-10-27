using System.ComponentModel.DataAnnotations;

namespace BankingPlatform.Api.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword),
        ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}