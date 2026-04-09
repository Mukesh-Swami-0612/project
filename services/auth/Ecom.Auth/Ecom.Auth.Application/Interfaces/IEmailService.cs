namespace Ecom.Auth.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken);
}
