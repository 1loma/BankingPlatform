using BankingPlatform.Api.Models.Enums;

namespace BankingPlatform.Api.Models;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Unique customer-visible identifier
    public string AccountNumber { get; set; } = string.Empty;

    public AccountType AccountType { get; set; }

    public string Currency { get; set; } = "USD";

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public DateTimeOffset OpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ClosedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // One account may have multiple customer owners
    public ICollection<AccCus> AccountCustomers { get; set; } = new List<AccCus>();
}