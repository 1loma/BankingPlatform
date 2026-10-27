using BankingPlatform.Api.Models.Enums;

namespace BankingPlatform.Api.Models;

public class AccCus
{
    public int CustomerId { get; set; }

    // Account IDs use Guid
    public Guid AccountId { get; set; }

    public AccountPermission Permission { get; set; } = AccountPermission.Owner;

    public DateTimeOffset AddedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // EF Core navigation properties
    public Customer Customer { get; set; } = null!;
    public Account Account { get; set; } = null!;
}