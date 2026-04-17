using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using FoodFrenzy.Models.ViewModels;
using System.Threading.Tasks;

namespace FoodFrenzy.Controllers
{
    public class ContactController : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IEmailSender emailSender, ILogger<ContactController> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ContactViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactViewModel model)
        {
            _logger.LogInformation("Contact form submission received");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed");
                return View(model);
            }

            // Validate email is not empty
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email address is required");
                return View(model);
            }

            try
            {
                _logger.LogInformation($"Processing contact form for: {model.Email}");

                // Prepare email content for admin
                var subject = $"[FoodFrenzy Contact] {GetSubjectText(model.Subject)} - {model.FirstName} {model.LastName}";
                var adminEmail = "arshadhaseeb901@gmail.com";

                // Validate admin email
                if (string.IsNullOrWhiteSpace(adminEmail))
                {
                    _logger.LogError("Admin email is not configured");
                    throw new Exception("Admin email is not configured.");
                }

                var message = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <h2 style='color: #333;'>New Contact Form Submission</h2>
                    
                    <table style='border-collapse: collapse; width: 100%;'>
                        <tr>
                            <td style='padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9; width: 150px;'><strong>Name:</strong></td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>{model.FirstName} {model.LastName}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9;'><strong>Email:</strong></td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>{model.Email}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9;'><strong>Phone:</strong></td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>{model.Phone ?? "Not provided"}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9;'><strong>Subject:</strong></td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>{GetSubjectText(model.Subject)}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border: 1px solid #ddd; background-color: #f9f9f9;'><strong>Newsletter:</strong></td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>{(model.SubscribeToNewsletter ? "Subscribed" : "Not subscribed")}</td>
                        </tr>
                    </table>

                    <h3 style='color: #333; margin-top: 20px;'>Message:</h3>
                    <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #4361ee; margin: 10px 0;'>
                        {model.Message.Replace("\n", "<br>")}
                    </div>

                    <hr style='margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>
                        This message was sent from the FoodFrenzy contact form on {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}
                    </p>
                </body>
                </html>";

                // Send email to admin
                _logger.LogInformation($"Sending email to admin: {adminEmail}");
                await _emailSender.SendEmailAsync(adminEmail, subject, message);
                _logger.LogInformation($"Admin email sent successfully");

                // Send auto-reply to the user
                var autoReplySubject = "Thank You for Contacting FoodFrenzy";
                var autoReplyMessage = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <h2 style='color: #333;'>Thank You for Contacting FoodFrenzy!</h2>
                    
                    <p>Dear {model.FirstName},</p>
                    
                    <p>Thank you for reaching out to us. We have received your message and will get back to you within 24-48 hours.</p>
                    
                    <div style='background-color: #f9f9f9; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <strong>Reference Information:</strong><br>
                        • Subject: {GetSubjectText(model.Subject)}<br>
                        • Submitted on: {DateTime.Now.ToString("MMMM dd, yyyy HH:mm")}
                    </div>
                    
                    <p>Best regards,<br>
                    <strong>FoodFrenzy Customer Support Team</strong></p>
                    
                    <hr style='margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>
                        This is an automated response. Please do not reply to this email.
                    </p>
                </body>
                </html>";

                _logger.LogInformation($"Sending auto-reply to user: {model.Email}");
                await _emailSender.SendEmailAsync(model.Email, autoReplySubject, autoReplyMessage);
                _logger.LogInformation($"Auto-reply email sent successfully");

                // Clear the form by redirecting to GET action
                TempData["SuccessMessage"] = "Thank you for your message! We've sent a confirmation to your email and will get back to you soon.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending contact form email from {model.Email}");

                // Add error to model state to display on page
                ModelState.AddModelError("", $"An error occurred while sending your message: {ex.Message}");

                // Return to view with current model to preserve entered data
                return View(model);
            }
        }

        private string GetSubjectText(string subjectCode)
        {
            return subjectCode switch
            {
                "general" => "General Inquiry",
                "product" => "Product Question",
                "order" => "Order Issue",
                "return" => "Return & Exchange",
                "shipping" => "Shipping Information",
                "feedback" => "Feedback",
                "other" => "Other",
                _ => subjectCode
            };
        }
    }
}