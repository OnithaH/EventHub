using EventHub.Models.ViewModels;
using EventHub.Models.Entities;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EventHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserService userService, ILogger<AccountController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            // Check if user is already logged in
            if (IsUserLoggedIn())
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                return RedirectBasedOnRole(userRole);
            }

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userService.GetUserByEmailAsync(model.Email);

                    if (user != null && user.IsActive && await _userService.ValidateUserAsync(model.Email, model.Password))
                    {
                        // Check if the role matches
                        if (user.Role.ToString() == model.Role)
                        {
                            // Set session variables with additional security
                            HttpContext.Session.SetString("UserId", user.Id.ToString());
                            HttpContext.Session.SetString("UserEmail", user.Email);
                            HttpContext.Session.SetString("UserName", user.Name);
                            HttpContext.Session.SetString("UserRole", user.Role.ToString());
                            HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString());

                            // Set remember me cookie if requested
                            if (model.RememberMe)
                            {
                                var cookieOptions = new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = Request.IsHttps,
                                    SameSite = SameSiteMode.Strict,
                                    Expires = DateTime.UtcNow.AddDays(30)
                                };
                                Response.Cookies.Append("RememberMe", user.Id.ToString(), cookieOptions);
                            }

                            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

                            TempData["SuccessMessage"] = $"Welcome back, {user.Name}!";

                            // Redirect based on role
                            return RedirectBasedOnRole(user.Role.ToString());
                        }
                        else
                        {
                            ModelState.AddModelError("", "Invalid role selected for this account");
                            _logger.LogWarning("Invalid role attempt for user {Email}", model.Email);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid email or password");
                        _logger.LogWarning("Failed login attempt for email {Email}", model.Email);

                        // Add delay to prevent brute force attacks
                        await Task.Delay(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
            }

            return View(model);
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            // Check if user is already logged in
            if (IsUserLoggedIn())
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                return RedirectBasedOnRole(userRole);
            }

            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Additional server-side validation
                    if (!IsValidEmail(model.Email))
                    {
                        ModelState.AddModelError("Email", "Please enter a valid email address");
                        return View(model);
                    }

                    if (!IsValidPassword(model.Password))
                    {
                        ModelState.AddModelError("Password", "Password must be at least 6 characters long and contain a mix of characters");
                        return View(model);
                    }

                    // Check if email already exists
                    var existingUser = await _userService.GetUserByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "This email is already registered");
                        return View(model);
                    }

                    // Validate company name for organizers
                    if (model.Role == UserRole.Organizer && string.IsNullOrWhiteSpace(model.Company))
                    {
                        ModelState.AddModelError("Company", "Company name is required for organizers");
                        return View(model);
                    }

                    // Create new user
                    var user = new User
                    {
                        Name = model.Name.Trim(),
                        Email = model.Email.Trim().ToLowerInvariant(),
                        Password = model.Password, // Will be hashed in service
                        Phone = model.Phone?.Trim(),
                        Role = model.Role,
                        Company = model.Company?.Trim(),
                        LoyaltyPoints = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _userService.CreateUserAsync(user);

                    _logger.LogInformation("New user registered: {UserId} - {Email} - {Role}", user.Id, user.Email, user.Role);

                    // Auto-login after registration
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserName", user.Name);
                    HttpContext.Session.SetString("UserRole", user.Role.ToString());
                    HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString());

                    TempData["SuccessMessage"] = "Registration successful! Welcome to EventHub.";

                    // Send welcome email (implement if needed)
                    // await _emailService.SendWelcomeEmailAsync(user);

                    return RedirectBasedOnRole(user.Role.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var userId = HttpContext.Session.GetString("UserId");

            // Clear session
            HttpContext.Session.Clear();

            // Clear remember me cookie
            Response.Cookies.Delete("RememberMe");

            _logger.LogInformation("User {UserId} logged out", userId);

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

            try
            {
                var user = await _userService.GetUserByIdAsync(int.Parse(userId));
                if (user == null || !user.IsActive)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user {UserId}", userId);
                TempData["ErrorMessage"] = "Error loading your profile. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/ForgotPassword (placeholder for future implementation)
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword (placeholder for future implementation)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Please enter your email address");
                return View();
            }

            try
            {
                var user = await _userService.GetUserByEmailAsync(email);

                // Always show success message for security (don't reveal if email exists)
                TempData["SuccessMessage"] = "If an account with that email exists, we've sent password reset instructions.";

                if (user != null && user.IsActive)
                {
                    // TODO: Implement password reset email
                    _logger.LogInformation("Password reset requested for user {UserId}", user.Id);
                }

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset for email {Email}", email);
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return View();
            }
        }

        // Helper Methods
        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        private int GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out int userId) ? userId : 0;
        }

        private IActionResult RedirectBasedOnRole(string role)
        {
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Organizer" => RedirectToAction("Dashboard", "Organizer"),
                "Customer" => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailAddress = new EmailAddressAttribute();
                return emailAddress.IsValid(email);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Minimum 6 characters
            if (password.Length < 6)
                return false;

            // At least one letter and one number (basic requirement)
            bool hasLetter = password.Any(char.IsLetter);
            bool hasDigit = password.Any(char.IsDigit);

            return hasLetter && hasDigit;
        }

        // Session management for security
        private bool IsSessionValid()
        {
            var loginTime = HttpContext.Session.GetString("LoginTime");
            if (string.IsNullOrEmpty(loginTime))
                return false;

            if (DateTime.TryParse(loginTime, out DateTime login))
            {
                // Session expires after 8 hours of inactivity
                return DateTime.UtcNow.Subtract(login).TotalHours < 8;
            }

            return false;
        }

        // Check for remember me cookie on app startup
        private async Task<bool> CheckRememberMeCookie()
        {
            if (Request.Cookies.TryGetValue("RememberMe", out string userIdStr))
            {
                if (int.TryParse(userIdStr, out int userId))
                {
                    try
                    {
                        var user = await _userService.GetUserByIdAsync(userId);
                        if (user != null && user.IsActive)
                        {
                            // Restore session
                            HttpContext.Session.SetString("UserId", user.Id.ToString());
                            HttpContext.Session.SetString("UserEmail", user.Email);
                            HttpContext.Session.SetString("UserName", user.Name);
                            HttpContext.Session.SetString("UserRole", user.Role.ToString());
                            HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString());
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking remember me cookie for user {UserId}", userId);
                    }
                }

                // Invalid cookie, remove it
                Response.Cookies.Delete("RememberMe");
            }
            return false;
        }


    }
}