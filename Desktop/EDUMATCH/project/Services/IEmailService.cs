namespace EduMatch.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink);
    Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink);
}
