using BankingPlatform.Api.Data;
using BankingPlatform.Api.DTOs.Auth;
using BankingPlatform.Api.Models;
using BankingPlatform.Api.Models.Identity;
using BankingPlatform.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BankingDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        BankingDbContext context,
        EmailService emailService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim();

        // Create an Identity user object
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        // Keep both database inserts in a single transaction
        await using (var transaction =
            await _context.Database.BeginTransactionAsync())
        {
            // Validate password, hash it, and save the user
            var result = await _userManager.CreateAsync(
                user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(
                    result.Errors.Select(e => e.Description));
            }

            // Create the related banking customer profile
            var customer = new Customer
            {
                UserId = user.Id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim()
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Both records are now permanently saved
            await transaction.CommitAsync();
        }

        // Generate an email verification token
        var token =
            await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Convert token to a URL-safe string
        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));

        // Generate the complete confirmation URL
        var confirmationUrl = Url.ActionLink(
            nameof(ConfirmEmail),
            "Auth",
            new { userId = user.Id, token = encodedToken },
            Request.Scheme);

        if (confirmationUrl == null)
        {
            _logger.LogError( "Could not generate email confirmation URL.");

            return StatusCode(500, "Account created, but confirmation link generation failed.");
        }

        // Email failure must not undo an already-created user
        try
        {
            await _emailService.SendAsync(
                user.Email!,
                "Confirm your BankingPlatform email",
                $"Confirm your email using this link:\n{confirmationUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration confirmation email.");

            return StatusCode(503, new { Message = "Account created, but confirmation email could not be sent." });
        }

        return Ok(new
        {
            Message = "Registration successful. Check your email to confirm your account."
        });
    }

    // GET /api/auth/confirm-email?userId=...&token=...
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        string userId, string token)
    {
        // Check required parameters
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Invalid confirmation link.");
        }

        // Find the user in AspNetUsers
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return BadRequest("Invalid confirmation link.");
        }

        string decodedToken;

        try
        {
            // Restore the original Identity token
            decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return BadRequest("Invalid confirmation token.");
        }

        // Validate token and set EmailConfirmed = true
        var result = await _userManager.ConfirmEmailAsync(
            user, decodedToken);

        if (!result.Succeeded)
        {
            return BadRequest("Invalid or expired confirmation link.");
        }

        return Ok("Email confirmed successfully.");
    }
}