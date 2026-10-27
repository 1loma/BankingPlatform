using BankingPlatform.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BankingPlatform.Api.DTOs.Accounts;

public class CreateAccountRequest
{
    [Required]
    [EnumDataType(typeof(AccountType))]
    public AccountType? AccountType { get; set; }
}