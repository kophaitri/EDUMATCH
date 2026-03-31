using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EduMatch.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly EduMatchDbContext _db;

    public EmailService(IConfiguration config, ILogger<EmailService> logger, EduMatchDbContext db)
    {
        _config = config;
        _logger = logger;
        _db = db;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        var subject = "Đặt lại mật khẩu EduMatch";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #4f46e5;">Đặt lại mật khẩu</h2>
                <p>Xin chào <strong>{toName}</strong>,</p>
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản EduMatch của bạn.</p>
                <p>Nhấn vào nút bên dưới để đặt lại mật khẩu. Link này sẽ hết hạn sau <strong>1 giờ</strong>.</p>
                <div style="text-align: center; margin: 30px 0;">
                    <a href="{resetLink}"
                       style="background-color: #4f46e5; color: white; padding: 12px 24px;
                              text-decoration: none; border-radius: 6px; font-size: 16px;">
                        Đặt lại mật khẩu
                    </a>
                </div>
                <p style="color: #6b7280; font-size: 14px;">
                    Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.
                    Mật khẩu của bạn sẽ không thay đổi.
                </p>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;">
                <p style="color: #9ca3af; font-size: 12px;">EduMatch – Kết nối học sinh và gia sư</p>
            </div>
            """;

        await SendEmailAsync(toEmail, toName, subject, body);
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink)
    {
        var subject = "Xác minh tài khoản EduMatch";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #4f46e5;">Xác minh tài khoản</h2>
                <p>Xin chào <strong>{toName}</strong>,</p>
                <p>Cảm ơn bạn đã đăng ký tài khoản EduMatch. Nhấn vào nút bên dưới để xác minh địa chỉ email của bạn.</p>
                <div style="text-align: center; margin: 30px 0;">
                    <a href="{confirmLink}"
                       style="background-color: #4f46e5; color: white; padding: 12px 24px;
                              text-decoration: none; border-radius: 6px; font-size: 16px;">
                        Xác minh email
                    </a>
                </div>
                <p style="color: #6b7280; font-size: 14px;">
                    Nếu bạn không thực hiện đăng ký, hãy bỏ qua email này.
                </p>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;">
                <p style="color: #9ca3af; font-size: 12px;">EduMatch – Kết nối học sinh và gia sư</p>
            </div>
            """;

        await SendEmailAsync(toEmail, toName, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        bool isSuccess = false;
        string? errorMessage = null;

        try
        {
            var smtpHost = _config["Email:SmtpHost"]!;
            var smtpPort = int.Parse(_config["Email:SmtpPort"]!);
            var username = _config["Email:Username"]!;
            var password = _config["Email:Password"]!;
            var fromName = _config["Email:FromName"]!;
            var fromAddress = _config["Email:FromAddress"]!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            isSuccess = true;
            _logger.LogInformation("Email '{Subject}' sent to {Email}.", subject, toEmail);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogError(ex, "Failed to send email '{Subject}' to {Email}.", subject, toEmail);
            throw;
        }
        finally
        {
            // Luôn ghi log kết quả gửi email vào EmailLog (kể cả thất bại)
            try
            {
                _db.EmailLogs.Add(new EmailLog
                {
                    ToEmail = toEmail,
                    Subject = subject,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    SentAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Could not save EmailLog for {Email}.", toEmail);
            }
        }
    }
}
