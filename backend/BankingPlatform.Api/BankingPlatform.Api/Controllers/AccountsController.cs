using BankingPlatform.Api.Data;
using BankingPlatform.Api.DTOs.Accounts;
using BankingPlatform.Api.Models;
using BankingPlatform.Api.Models.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.IdentityModel.Tokens.Jwt;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly BankingDbContext _context;

    public AccountsController(BankingDbContext context)
    {
        _context = context;
    }

    // POST /api/Accounts
    [HttpPost]
    public async Task<IActionResult> CreateAccount(
        CreateAccountRequest request)
    {
        // Read the authenticated user's ID from JWT
        var userId = User.FindFirst(
            JwtRegisteredClaimNames.Sub)?.Value;

        if (userId == null)
            return Unauthorized();

        // Find this user's active, confirmed Identity account
        var user = await _context.Users
            .Where(u => u.Id == userId &&
                        u.IsActive &&
                        u.EmailConfirmed)
            .FirstOrDefaultAsync();

        if (user == null)
            return Unauthorized();

        // Each Identity user has a linked Customer profile
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
            return BadRequest("Customer profile not found.");

        if (request.AccountType == null ||
            !Enum.IsDefined(request.AccountType.Value))
            return BadRequest("Invalid account type.");

        // Create a new simulated bank account
        var account = new Account
        {
            AccountNumber = "BP" + Guid.NewGuid()
                .ToString("N")[..24].ToUpperInvariant(),

            AccountType = request.AccountType.Value,
            Currency = "USD",
            Status = AccountStatus.Active
        };

        // Link this customer as an Owner of the account
        var ownership = new AccCus
        {
            CustomerId = customer.Id,
            AccountId = account.Id,
            Permission = AccountPermission.Owner
        };

        // Save the account and ownership together
        _context.Accounts.Add(account);
        _context.AccountCustomers.Add(ownership);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAccountById),
            new { id = account.Id },
            new
            {
                account.Id,
                account.AccountNumber,
                account.AccountType,
                account.Currency,
                account.Status,
                account.OpenedAtUtc
            });
    }

    // GET /api/Accounts
    [HttpGet]
    public async Task<IActionResult> GetMyAccounts()
    {
        var userId = User.FindFirst(
            JwtRegisteredClaimNames.Sub)?.Value;

        if (userId == null)
            return Unauthorized();

        var accounts = await _context.AccountCustomers
            .AsNoTracking()
            .Where(ac =>
                ac.Customer.UserId == userId &&
                ac.Customer.User.IsActive &&
                ac.Customer.User.EmailConfirmed)
            .Select(ac => new
            {
                ac.Account.Id,
                ac.Account.AccountNumber,
                ac.Account.AccountType,
                ac.Account.Currency,
                ac.Account.Status,
                ac.Permission,
                ac.Account.OpenedAtUtc
            })
            .ToListAsync();

        return Ok(accounts);
    }

    // GET /api/Accounts/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var userId = User.FindFirst(
            JwtRegisteredClaimNames.Sub)?.Value;

        if (userId == null)
            return Unauthorized();

        // Both account ID and ownership must match
        var account = await _context.AccountCustomers
            .AsNoTracking()
            .Where(ac =>
                ac.AccountId == id &&
                ac.Customer.UserId == userId &&
                ac.Customer.User.IsActive &&
                ac.Customer.User.EmailConfirmed)
            .Select(ac => new
            {
                ac.Account.Id,
                ac.Account.AccountNumber,
                ac.Account.AccountType,
                ac.Account.Currency,
                ac.Account.Status,
                ac.Permission,
                ac.Account.OpenedAtUtc
            })
            .FirstOrDefaultAsync();

        if (account == null)
            return NotFound("Account not found.");

        return Ok(account);
    }
}