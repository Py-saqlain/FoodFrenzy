using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace FoodFrenzy.Services
{
    public class RealEmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<RealEmailSender> _logger;

        public RealEmailSender(IOptions<EmailSettings> emailSettings, ILogger<RealEmailSender> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogError("Email address is null or empty");
                throw new ArgumentException("Email address cannot be empty", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = "No Subject";
                _logger.LogWarning("Email subject was empty, using default");
            }

            if (string.IsNullOrWhiteSpace(htmlMessage))
            {
                htmlMessage = "<p>No message content</p>";
                _logger.LogWarning("Email message was empty, using default");
            }

            _logger.LogInformation($"Sending email to: {email}, Subject: {subject}");

            // Validate email configuration
            if (string.IsNullOrWhiteSpace(_emailSettings.SenderEmail))
            {
                _logger.LogError("SenderEmail is not configured in appsettings.json");
                throw new InvalidOperationException("Email sender is not configured. Check appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(_emailSettings.SenderPassword))
            {
                _logger.LogError("SenderPassword is not configured in appsettings.json");
                throw new InvalidOperationException("Email password is not configured. Check appsettings.json");
            }

            try
            {
                var fromEmail = _emailSettings.SenderEmail.Trim();
                var fromName = _emailSettings.SenderName?.Trim() ?? "FoodFrenzy Support";

                _logger.LogInformation($"Using SMTP: {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}");

                using var mail = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };

                // Add recipient with validation
                var toEmail = email.Trim();
                mail.To.Add(new MailAddress(toEmail));

                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(
                        fromEmail,
                        _emailSettings.SenderPassword.Trim()
                    ),
                    EnableSsl = _emailSettings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 30000
                };

                // Set security protocol for Gmail
                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls12 |
                    System.Net.SecurityProtocolType.Tls11 |
                    System.Net.SecurityProtocolType.Tls;

                _logger.LogInformation("Attempting to send email...");
                await smtpClient.SendMailAsync(mail);
                _logger.LogInformation($"Email sent successfully to {email}");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, $"SMTP Error sending email to {email}");
                throw new Exception($"SMTP Error: {ex.Message}. Please check your email configuration and credentials.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}");
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string SenderName { get; set; } = "FoodFrenzy Support";
        public string AdminEmail { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}