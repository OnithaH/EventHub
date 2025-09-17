using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
