using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    public class EventController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
