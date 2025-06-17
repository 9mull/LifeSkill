using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace LifeSkill.Web.Services;

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string message);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword)
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
            Subject = subject,
            Body = message,
            IsBodyHtml = true
        };
        mailMessage.To.Add(email);

        await client.SendMailAsync(mailMessage);
    }
} 