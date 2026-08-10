using LeaveManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace LeaveManagement.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
        var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
        var senderEmail = _config["EmailSettings:SenderEmail"];
        var password = _config["EmailSettings:Password"];

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("EmailSettings are missing or incomplete. Logging email to console.");
            Console.WriteLine($"[MOCK EMAIL TO {to}]: {subject}\n{body}");
            return;
        }

        try
        {
            using var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true,
                Timeout = 5000 
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "Leave Management System"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);
            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Successfully sent email to {ToAddress}", to);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not dispatch email to {ToAddress} via SMTP (Host firewall may be blocking port {Port}): {Message}",
                to, port, ex.Message);
        }
    }
}