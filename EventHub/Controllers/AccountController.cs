using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
