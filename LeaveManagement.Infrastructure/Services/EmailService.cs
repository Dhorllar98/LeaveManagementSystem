using LeaveManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using Task = System.Threading.Tasks.Task;

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
        var apiKey = _config["EmailSettings:BrevoApiKey"];
        var senderEmail = _config["EmailSettings:SenderEmail"] ?? "noreply@leavemanagementsystem.com";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Brevo API Key is missing. Logging email to console.");
            Console.WriteLine($"[MOCK EMAIL TO {to}]: {subject}\n{body}");
            return;
        }

        try
        {
            var apiConfig = new Configuration();
            apiConfig.ApiKey["api-key"] = apiKey;

            var apiInstance = new TransactionalEmailsApi(apiConfig);
            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender("Leave Management System", senderEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(to) },
                Subject = subject,
                HtmlContent = body
            };

            await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation("Successfully sent email to {ToAddress} via Brevo HTTP API", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToAddress} via Brevo API", to);
        }
    }
}