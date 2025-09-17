using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    public class NewsletterController : Controller
    {
        private readonly ILogger<NewsletterController> _logger;

        public NewsletterController(ILogger<NewsletterController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
                {
                    return Json(new { success = false, message = "Please enter a valid email address." });
                }

                // Implement newsletter subscription logic here
                _logger.LogInformation("Newsletter subscription for: {Email}", email);

                return Json(new { success = true, message = "Thank you for subscribing!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Newsletter subscription error");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}