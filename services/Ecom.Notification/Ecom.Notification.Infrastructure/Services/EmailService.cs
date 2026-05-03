using Ecom.Notification.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using Polly;
using Polly.Retry;

namespace Ecom.Notification.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
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

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var senderEmail = _config["EmailSettings:Email"];
            var password = _config["EmailSettings:Password"];

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
            {
                _logger.LogError("Email configuration (Email or Password) is missing in appsettings.json.");
                return false;
            }

            // Execute SMTP operations with retry policy
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(senderEmail, password);
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var mail = new MailMessage(senderEmail, toEmail, subject, body))
                    {
                        // If caller passes an HTML template, ensure it's rendered as HTML by email clients.
                        // (Without this, Gmail will show raw HTML tags.)
                        mail.IsBodyHtml = LooksLikeHtml(body);
                        mail.BodyEncoding = System.Text.Encoding.UTF8;
                        mail.SubjectEncoding = System.Text.Encoding.UTF8;

                        // Always add a plain-text alternative (best-effort) for clients that prefer text/plain.
                        // This also improves deliverability and accessibility.
                        var plainText = StripHtmlToText(body);
                        if (!string.IsNullOrWhiteSpace(plainText))
                        {
                            mail.AlternateViews.Add(
                                AlternateView.CreateAlternateViewFromString(
                                    plainText,
                                    System.Text.Encoding.UTF8,
                                    "text/plain"));
                        }

                        if (mail.IsBodyHtml)
                        {
                            mail.AlternateViews.Add(
                                AlternateView.CreateAlternateViewFromString(
                                    body,
                                    System.Text.Encoding.UTF8,
                                    "text/html"));
                        }

                        await smtp.SendMailAsync(mail);
                    }
                }
            });

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("EMAIL ERROR:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);

            _logger.LogError(ex, "Failed to send email to {Email} after all retry attempts. Error: {Message}", 
                toEmail, ex.Message);

            return false;
        }
    }

    private static bool LooksLikeHtml(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        // Quick heuristic: treat as HTML if it contains typical tags or a doctype.
        var s = body.AsSpan().TrimStart();
        return s.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
               || body.Contains("<body", StringComparison.OrdinalIgnoreCase)
               || body.Contains("</", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripHtmlToText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // Very small, dependency-free plain text fallback.
        // This is not a full HTML parser, but good enough for readable plain-text alternatives.
        var text = html
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ");

        // Remove tags.
        var result = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");
        // Decode entities.
        result = System.Net.WebUtility.HtmlDecode(result);
        // Collapse whitespace.
        result = System.Text.RegularExpressions.Regex.Replace(result, "\\s{2,}", " ").Trim();
        return result;
    }
}