using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
