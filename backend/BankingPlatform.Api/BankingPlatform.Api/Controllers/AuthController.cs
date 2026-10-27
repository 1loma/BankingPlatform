using BankingPlatform.Api.Data;
using BankingPlatform.Api.DTOs.Auth;
using BankingPlatform.Api.Models;
using BankingPlatform.Api.Models.Identity;
using BankingPlatform.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Net;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BankingDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly JwtService _jwtService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    public AuthController(
        UserManager<ApplicationUser> userManager,
        BankingDbContext context,
        EmailService emailService,
        ILogger<AuthController> logger, 
        SignInManager<ApplicationUser> signInManager,
        JwtService jwtService)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _signInManager = signInManager;
        _jwtService = jwtService;
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




    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // Search for the registered user by email
        var user = await _userManager.FindByEmailAsync(
            request.Email.Trim());

        if (user == null)
            return Unauthorized("Invalid email or password.");

        // Block disabled banking users
        if (!user.IsActive)
            return Unauthorized("Account is unavailable.");

        // Identity checks the password and handles lockout attempts
        var result = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Unauthorized("Account temporarily locked.");

        if (!result.Succeeded)
            return Unauthorized("Invalid credentials or unconfirmed email.");

        // Only authenticated users receive a JWT
        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresInSeconds = 900
        });
    }

    // GET /api/auth/me
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        // Read the user ID from the validated JWT
        var userId = User.FindFirst(
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (userId == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || !user.IsActive ||
            !await _userManager.IsEmailConfirmedAsync(user))
        {
            return Unauthorized();
        }

        // Return only safe profile information
        return Ok(new
        {
            user.Id,
            user.Email,
            user.EmailConfirmed,
            user.CreatedAt
        });
    }



    // POST /api/Auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request)
    {
        const string message =
            "If an eligible account exists, reset instructions will be sent.";

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (user == null || !user.IsActive ||
            !await _userManager.IsEmailConfirmedAsync(user))
            return Ok(new { Message = message });

        // Identity generates a protected, time-limited reset token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));

        // Generate a clickable link to our password-reset page
        var link = Url.ActionLink(
            nameof(ResetPasswordPage),
            "Auth",
            new { email = user.Email, token = encodedToken },
            Request.Scheme);

        if (link == null)
            return StatusCode(500, "Could not create reset link.");

        try
        {
            await _emailService.SendAsync(
                user.Email!,
                "Reset your BankingPlatform password",
                $"Click this link to reset your password:\n{link}\n\n" +
                "If you did not request this, ignore this email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email.");
        }

        return Ok(new { Message = message });
    }


    // GET /api/Auth/reset-password-page?email=...&token=...
    [HttpGet("reset-password-page")]
    public IActionResult ResetPasswordPage(string email, string token)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(token))
            return BadRequest("Invalid reset link.");

        // Encode values before placing them inside HTML
        var safeEmail = WebUtility.HtmlEncode(email);
        var safeToken = WebUtility.HtmlEncode(token);

        // A temporary form until we build the React frontend
        var html = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <title>BankingPlatform - Reset Password</title>
                <meta name="referrer" content="no-referrer">
            </head>
            <body style="font-family:Arial;max-width:400px;margin:80px auto;">
                <h2>Reset your password</h2>

                <form method="post" action="/api/Auth/reset-password">
                    <input type="hidden" name="email" value="{{safeEmail}}">
                    <input type="hidden" name="token" value="{{safeToken}}">

                    <label>New Password</label>
                    <input type="password" name="NewPassword"
                           required minlength="6" style="display:block;margin:10px 0;">

                    <label>Confirm Password</label>
                    <input type="password" name="ConfirmPassword"
                           required style="display:block;margin:10px 0;">

                    <button type="submit">Reset Password</button>
                </form>
            </body>
            </html>
            """;

        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";

        return Content(html, "text/html");
    }


    // POST /api/Auth/reset-password
    [HttpPost("reset-password")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> ResetPassword(
        [FromForm] ResetPasswordRequest request,
        [FromForm] string email,
        [FromForm] string token)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user == null || !user.IsActive)
            return BadRequest("Invalid or expired reset link.");

        string decodedToken;

        try
        {
            decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return BadRequest("Invalid reset token.");
        }

        // Verify emailed token and securely update password
        var result = await _userManager.ResetPasswordAsync(
            user, decodedToken, request.NewPassword);

        if (!result.Succeeded)
            return BadRequest("Invalid reset token or password.");

        Response.Headers.CacheControl = "no-store";

        return Content(
            "<h2>Password reset successfully!</h2><p>You can now log in.</p>",
            "text/html");
    }

}