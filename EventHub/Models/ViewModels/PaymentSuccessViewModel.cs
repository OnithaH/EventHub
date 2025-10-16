using Microsoft.AspNetCore.Mvc;

namespace EventHub.Models.ViewModels
{
    public class PaymentSuccessViewModel : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
