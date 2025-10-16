using Microsoft.AspNetCore.Mvc;

namespace EventHub.Models.ViewModels
{
    public class CheckoutViewModel : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
