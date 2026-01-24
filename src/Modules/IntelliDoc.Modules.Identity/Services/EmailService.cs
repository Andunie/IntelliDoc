using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace IntelliDoc.Modules.Identity.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_config["Email:From"] ?? "noreply@intellidoc.com"));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();
        // Gmail için: smtp.gmail.com, Port: 587, SSL: false (StartTls)
        // Kendi SMTP ayarlarınızı appsettings.json'dan okuyacağız
        await smtp.ConnectAsync(
            _config["Email:Host"] ?? "smtp.gmail.com",
            int.Parse(_config["Email:Port"] ?? "587"),
            MailKit.Security.SecureSocketOptions.StartTls
        );

        await smtp.AuthenticateAsync(
            _config["Email:User"],
            _config["Email:Password"]
        );

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
