using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BankingPlatform.Api.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string to, string subject, string message)
    {
        // Create the email
        var email = new MimeMessage();

        email.From.Add(MailboxAddress.Parse(
            _configuration["Email:From"]!));

        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;

        // Email body (plain text)
        email.Body = new TextPart("plain")
        {
            Text = message
        };

        using var smtp = new SmtpClient();

        // Connect securely to Gmail using STARTTLS
        await smtp.ConnectAsync(
            _configuration["Email:Host"]!,
            int.Parse(_configuration["Email:Port"]!),
            SecureSocketOptions.StartTls);

        // Authenticate using Gmail App Password
        await smtp.AuthenticateAsync(
            _configuration["Email:From"]!,
            _configuration["Email:AppPassword"]!);

        // Send the email
        await smtp.SendAsync(email);

        // Close the SMTP connection
        await smtp.DisconnectAsync(true);
    }
}