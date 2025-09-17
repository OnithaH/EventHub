using EventHub.Models.ViewModels;
using EventHub.Models.Entities;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.GetUserByEmailAsync(model.Email);

                if (user != null && await _userService.ValidateUserAsync(model.Email, model.Password))
                {
                    // Check if the role matches
                    if (user.Role.ToString() == model.Role)
                    {
                        // Set session variables
                        HttpContext.Session.SetString("UserId", user.Id.ToString());
                        HttpContext.Session.SetString("UserEmail", user.Email);
                        HttpContext.Session.SetString("UserName", user.Name);
                        HttpContext.Session.SetString("UserRole", user.Role.ToString());

                        // Redirect based on role
                        return user.Role switch
                        {
                            UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                            UserRole.Organizer => RedirectToAction("Dashboard", "Organizer"),
                            UserRole.Customer => RedirectToAction("Index", "Home"),
                            _ => RedirectToAction("Index", "Home")
                        };
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid role selected for this account");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password");
                }
            }

            return View(model);
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _userService.GetUserByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered");
                    return View(model);
                }

                // Create new user
                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = model.Password, // Will be hashed in service
                    Phone = model.Phone,
                    Role = model.Role,
                    Company = model.Company,
                    LoyaltyPoints = 0,
                    IsActive = true
                };

                try
                {
                    await _userService.CreateUserAsync(user);

                    // Auto-login after registration
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserName", user.Name);
                    HttpContext.Session.SetString("UserRole", user.Role.ToString());

                    TempData["SuccessMessage"] = "Registration successful! Welcome to EventHub.";

                    return user.Role switch
                    {
                        UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                        UserRole.Organizer => RedirectToAction("Dashboard", "Organizer"),
                        UserRole.Customer => RedirectToAction("Index", "Home"),
                        _ => RedirectToAction("Index", "Home")
                    };
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                    // Log the exception in production
                }
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // Helper method to check if user is logged in
        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        // Helper method to get current user ID
        private int GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out int userId) ? userId : 0;
        }
    }
}