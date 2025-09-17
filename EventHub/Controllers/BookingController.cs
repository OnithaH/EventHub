using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
