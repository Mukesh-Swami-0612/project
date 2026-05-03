using System.Diagnostics;
using Ecom.Auth.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Polly;
using Polly.Retry;

namespace Ecom.Auth.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Configure retry policy: 3 attempts with exponential backoff (2s, 4s, 8s)
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Email send attempt {RetryCount} failed. Retrying in {RetryDelay}s. Error: {ErrorMessage}",
                        retryCount,
                        timeSpan.TotalSeconds,
                        exception.Message);
                });
    }

    public async Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken)
    {
        var frontendUrl = _configuration["App:FrontendUrl"]?.Trim();
        var verificationUrl = string.IsNullOrWhiteSpace(frontendUrl)
            ? null
            : $"{frontendUrl}/verify-email?code={verificationToken}";

        var subject = "Your verification code";
        var body = $@"
            <html>
            <body>
                <h2>Welcome to Ecommerce Platform, {userName}!</h2>
                <p>Use the 6-digit code below to verify your email address:</p>
                <p style='font-size:24px;font-weight:bold;letter-spacing:4px;'>{verificationToken}</p>
                {(verificationUrl != null ? $"<p>If you prefer, you can also open the link with the code prefilled:</p><p><a href='{verificationUrl}'>Verify my email</a></p>" : string.Empty)}
                <p>This code expires in 1 hour.</p>
                <p>If you didn't create an account, please ignore this email.</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
    {
        var frontendUrl = _configuration["App:FrontendUrl"]?.Trim();
        var resetUrl = string.IsNullOrWhiteSpace(frontendUrl)
            ? null
            : $"{frontendUrl}/reset-password?code={resetToken}";

        var subject = "Your password reset code";
        var body = $@"
            <html>
            <body>
                <h2>Password Reset Request</h2>
                <p>Hi {userName},</p>
                <p>Enter this 6-digit code to reset your password:</p>
                <p style='font-size:24px;font-weight:bold;letter-spacing:4px;'>{resetToken}</p>
                {(resetUrl != null ? $"<p>Or use this link with the code prefilled:</p><p><a href='{resetUrl}'>Reset password</a></p>" : string.Empty)}
                <p>This code expires in 1 hour.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var settings = GetEmailSettings();

        _logger.LogInformation(
            "Preparing email send Host={Host} Port={Port} User={User} EnableSsl={EnableSsl} To={To}",
            settings.SmtpHost, settings.SmtpPort, settings.SmtpUser, settings.EnableSsl, toEmail);

        try
        {
            var stopwatch = Stopwatch.StartNew();

            var mailMessage = new MimeMessage();
            mailMessage.From.Add(new MailboxAddress(settings.FromName ?? "Ecommerce Platform", settings.FromEmail!));
            mailMessage.To.Add(new MailboxAddress("", toEmail));
            mailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            mailMessage.Body = bodyBuilder.ToMessageBody();

            // Execute SMTP operations with retry policy
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using var client = new SmtpClient();
                client.Timeout = 15000;

                _logger.LogDebug("SMTP client created. Connecting/authenticating to {Host}:{Port} (SSL={EnableSsl})", 
                    settings.SmtpHost, settings.SmtpPort, settings.EnableSsl);

                var secureSocketOptions = settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secureSocketOptions);
                await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
                
                await client.SendAsync(mailMessage);
                await client.DisconnectAsync(true);
            });

            stopwatch.Stop();
            _logger.LogInformation("Email sent successfully to {Email} in {ElapsedMs}ms", toEmail, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via {Host}:{Port} after all retry attempts", 
                toEmail, settings.SmtpHost, settings.SmtpPort);
            throw;
        }
    }

    private EmailSettings GetEmailSettings()
    {
        var emailSection = _configuration.GetSection("Email");
        if (!emailSection.Exists())
        {
            throw new InvalidOperationException("Email configuration section 'Email' is missing.");
        }

        var settings = emailSection.Get<EmailSettings>() ?? new EmailSettings();

        settings.SmtpHost = settings.SmtpHost?.Trim();
        settings.SmtpUser = settings.SmtpUser?.Trim();
        settings.SmtpPassword = settings.SmtpPassword?.Replace(" ", string.Empty)?.Trim();
        settings.FromEmail = string.IsNullOrWhiteSpace(settings.FromEmail) ? settings.SmtpUser : settings.FromEmail?.Trim();
        settings.FromName = string.IsNullOrWhiteSpace(settings.FromName) ? "Ecommerce Platform" : settings.FromName?.Trim();

        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
            throw new InvalidOperationException("Email:SmtpHost is missing.");

        if (string.IsNullOrWhiteSpace(settings.SmtpUser))
            throw new InvalidOperationException("Email:SmtpUser is missing.");

        if (string.IsNullOrWhiteSpace(settings.SmtpPassword))
            throw new InvalidOperationException("Email:SmtpPassword is missing.");

        if (settings.SmtpPort <= 0)
            throw new InvalidOperationException("Email:SmtpPort is missing or invalid.");

        return settings;
    }

    public class EmailSettings
    {
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? SmtpUser { get; set; }
        public string? SmtpPassword { get; set; }
        public bool EnableSsl { get; set; } = true;
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
    }
}
