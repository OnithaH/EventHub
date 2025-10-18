using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Models.ViewModels;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EventHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(IUserService userService, ILogger<AccountController> logger, ApplicationDbContext context)
        {
            _userService = userService;
            _logger = logger;
            _context = context;
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

        // FIXED: AccountController.cs - Replace your Register POST method with this:

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║          REGISTRATION PROCESS STARTED                      ║");
            _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");

            _logger.LogInformation("📋 RECEIVED DATA:");
            _logger.LogInformation("   Name: {Name}", model.Name ?? "NULL");
            _logger.LogInformation("   Email: {Email}", model.Email ?? "NULL");
            _logger.LogInformation("   Role: {Role}", model.Role);
            _logger.LogInformation("   Phone: {Phone}", model.Phone ?? "NULL");

            _logger.LogInformation("🔍 MODEL STATE CHECK:");
            _logger.LogInformation("   IsValid: {IsValid}", ModelState.IsValid);

            // 🔧 FIX: Check ModelState validity first
            if (!ModelState.IsValid)
            {
                _logger.LogError("❌ MODEL STATE IS INVALID - VALIDATION ERRORS:");
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                foreach (var error in errors)
                {
                    foreach (var err in error.Errors)
                    {
                        _logger.LogError("   ❗ Field: {Field} → {ErrorMessage}", error.Key, err.ErrorMessage);
                    }
                }

                return View(model);
            }

            _logger.LogInformation("✅ MODEL STATE IS VALID - PROCEEDING");

            try
            {
                // 🔧 FIX: Validate password match (defensive check)
                if (model.Password != model.ConfirmPassword)
                {
                    _logger.LogError("❌ PASSWORDS DO NOT MATCH");
                    ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
                    return View(model);
                }

                // 🔧 FIX: Validate password length
                if (model.Password.Length < 6)
                {
                    _logger.LogError("❌ PASSWORD TOO SHORT");
                    ModelState.AddModelError("Password", "Password must be at least 6 characters");
                    return View(model);
                }

                // 🔧 FIX: Validate email format
                if (!IsValidEmail(model.Email))
                {
                    _logger.LogError("❌ INVALID EMAIL FORMAT");
                    ModelState.AddModelError("Email", "Please enter a valid email address");
                    return View(model);
                }

                // Check if email already exists
                var existingUser = await _userService.GetUserByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("⚠️ EMAIL ALREADY EXISTS: {Email}", model.Email);
                    ModelState.AddModelError("Email", "This email is already registered. Please log in or use a different email.");
                    return View(model);
                }

                // 🔧 FIX: Validate phone if provided
                if (!string.IsNullOrEmpty(model.Phone) && !IsValidPhone(model.Phone))
                {
                    _logger.LogError("❌ INVALID PHONE FORMAT");
                    ModelState.AddModelError("Phone", "Please enter a valid phone number");
                    return View(model);
                }

                // 🔧 FIX: Validate organizer company
                if (model.Role == UserRole.Organizer && string.IsNullOrEmpty(model.Company))
                {
                    _logger.LogError("❌ ORGANIZER WITHOUT COMPANY");
                    ModelState.AddModelError("Company", "Company name is required for organizers");
                    return View(model);
                }

                _logger.LogInformation("✅ ALL VALIDATIONS PASSED - CREATING USER");

                // Create user
                var user = new User
                {
                    Name = model.Name.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = model.Role,
                    Phone = model.Phone?.Trim(),
                    Company = model.Company?.Trim(),
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender?.Trim(),
                    City = model.City?.Trim(),
                    Interests = model.Interests?.Trim(),
                    Website = model.Website?.Trim(),
                    OrganizationType = model.OrganizationType?.Trim(),
                    Description = model.Description?.Trim(),
                    EmailNotifications = model.EmailNotifications,
                    SmsNotifications = model.SmsNotifications,
                    MarketingEmails = model.MarketingEmails,
                    LoyaltyPoints = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("👤 USER OBJECT CREATED:");
                _logger.LogInformation("   Name: {Name}", user.Name);
                _logger.LogInformation("   Email: {Email}", user.Email);
                _logger.LogInformation("   Role: {Role}", user.Role);

                // Register via service
                var result = await _userService.RegisterUserAsync(user);

                if (result)
                {
                    _logger.LogInformation("✅ USER REGISTERED SUCCESSFULLY");
                    _logger.LogInformation("🎉 REGISTRATION COMPLETED");
                    _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
                    _logger.LogInformation("║          REGISTRATION SUCCESSFUL                         ║");
                    _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");

                    TempData["SuccessMessage"] = "Registration successful! Please log in with your credentials.";
                    return RedirectToAction("Login");
                }
                else
                {
                    _logger.LogError("❌ USER SERVICE REGISTRATION FAILED");
                    ModelState.AddModelError("", "Registration failed. Please try again.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("╔═══════════════════════════════════════════════════════════╗");
                _logger.LogError("║          ERROR IN REGISTRATION PROCESS                   ║");
                _logger.LogError("╚═══════════════════════════════════════════════════════════╝");
                _logger.LogError("❌ Exception: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("❌ Message: {Message}", ex.Message);
                _logger.LogError("❌ Stack: {StackTrace}", ex.StackTrace);

                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View(model);
            }
        }

        // 🔧 FIX: Helper validation methods
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

        private bool IsValidPhone(string phone)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
            return cleaned.Length >= 9 && cleaned.Length <= 15;
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

        /// <summary>
        /// GET: Display Edit Profile page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login");
                }

                var userId = int.Parse(userIdString);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                var viewModel = new EditProfileViewModel
                {
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    City = user.City,
                    Interests = user.Interests,
                    Company = user.Company,
                    OrganizationType = user.OrganizationType,
                    Website = user.Website,
                    Description = user.Description,
                    EmailNotifications = user.EmailNotifications,
                    SmsNotifications = user.SmsNotifications,
                    MarketingEmails = user.MarketingEmails,
                    Role = user.Role,
                    LoyaltyPoints = user.LoyaltyPoints,
                    CreatedAt = user.CreatedAt
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile page");
                TempData["ErrorMessage"] = "Unable to load profile.";
                return RedirectToAction("Login");
            }
        }
        /// <summary>
        /// POST: Update user profile
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditProfileViewModel model)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login");
                }

                var userId = int.Parse(userIdString);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }

                // Remove password validation if not changing password
                if (!model.IsPasswordChangeRequested())
                {
                    ModelState.Remove(nameof(model.CurrentPassword));
                    ModelState.Remove(nameof(model.NewPassword));
                    ModelState.Remove(nameof(model.ConfirmNewPassword));
                }

                if (!ModelState.IsValid)
                {
                    // Reload role-specific data
                    model.Role = user.Role;
                    model.LoyaltyPoints = user.LoyaltyPoints;
                    model.CreatedAt = user.CreatedAt;
                    return View(model);
                }

                // Check if email is being changed and already exists
                if (user.Email != model.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email && u.Id != userId);

                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "This email is already in use.");
                        model.Role = user.Role;
                        model.LoyaltyPoints = user.LoyaltyPoints;
                        model.CreatedAt = user.CreatedAt;
                        return View(model);
                    }
                }

                // ✅ TRACK CHANGES - Store what changed
                var changes = new List<object>();

                // Track basic info changes
                if (user.Name != model.Name)
                    changes.Add(new { field = "Name", oldValue = user.Name, newValue = model.Name });

                if (user.Email != model.Email)
                    changes.Add(new { field = "Email", oldValue = user.Email, newValue = model.Email });

                if (user.Phone != model.Phone)
                    changes.Add(new { field = "Phone Number", oldValue = user.Phone ?? "Not set", newValue = model.Phone ?? "Not set" });

                if (user.City != model.City)
                    changes.Add(new { field = "City", oldValue = user.City ?? "Not set", newValue = model.City ?? "Not set" });

                // Track personal info changes (for customers)
                if (user.DateOfBirth != model.DateOfBirth)
                    changes.Add(new { field = "Date of Birth", oldValue = user.DateOfBirth?.ToString("MMM dd, yyyy") ?? "Not set", newValue = model.DateOfBirth?.ToString("MMM dd, yyyy") ?? "Not set" });

                if (user.Gender != model.Gender)
                    changes.Add(new { field = "Gender", oldValue = user.Gender ?? "Not set", newValue = model.Gender ?? "Not set" });

                if (user.Interests != model.Interests)
                    changes.Add(new { field = "Interests", oldValue = user.Interests ?? "Not set", newValue = model.Interests ?? "Not set" });

                // Track company info changes (for organizers)
                if (user.Company != model.Company)
                    changes.Add(new { field = "Company Name", oldValue = user.Company ?? "Not set", newValue = model.Company ?? "Not set" });

                if (user.OrganizationType != model.OrganizationType)
                    changes.Add(new { field = "Organization Type", oldValue = user.OrganizationType ?? "Not set", newValue = model.OrganizationType ?? "Not set" });

                if (user.Website != model.Website)
                    changes.Add(new { field = "Website", oldValue = user.Website ?? "Not set", newValue = model.Website ?? "Not set" });

                if (user.Description != model.Description)
                    changes.Add(new { field = "Description", oldValue = user.Description ?? "Not set", newValue = model.Description ?? "Not set" });

                // Track notification preferences
                if (user.EmailNotifications != model.EmailNotifications)
                    changes.Add(new { field = "Email Notifications", oldValue = user.EmailNotifications ? "Enabled" : "Disabled", newValue = model.EmailNotifications ? "Enabled" : "Disabled" });

                if (user.SmsNotifications != model.SmsNotifications)
                    changes.Add(new { field = "SMS Notifications", oldValue = user.SmsNotifications ? "Enabled" : "Disabled", newValue = model.SmsNotifications ? "Enabled" : "Disabled" });

                if (user.MarketingEmails != model.MarketingEmails)
                    changes.Add(new { field = "Marketing Emails", oldValue = user.MarketingEmails ? "Enabled" : "Disabled", newValue = model.MarketingEmails ? "Enabled" : "Disabled" });

                // Handle password change if requested
                if (model.IsPasswordChangeRequested())
                {
                    // Verify current password
                    if (!_userService.VerifyPassword(model.CurrentPassword!, user.Password))
                    {
                        ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                        model.Role = user.Role;
                        model.LoyaltyPoints = user.LoyaltyPoints;
                        model.CreatedAt = user.CreatedAt;
                        return View(model);
                    }

                    // Hash and update new password
                    user.Password = _userService.HashPassword(model.NewPassword!);

                    // Add password change to changes list
                    changes.Add(new { field = "Password", oldValue = "••••••••", newValue = "••••••••" });
                }

                // Update user properties
                user.Name = model.Name;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.DateOfBirth = model.DateOfBirth;
                user.Gender = model.Gender;
                user.City = model.City;
                user.Interests = model.Interests;
                user.Company = model.Company;
                user.OrganizationType = model.OrganizationType;
                user.Website = model.Website;
                user.Description = model.Description;
                user.EmailNotifications = model.EmailNotifications;
                user.SmsNotifications = model.SmsNotifications;
                user.MarketingEmails = model.MarketingEmails;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Update session data
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserEmail", user.Email);

                // ✅ PASS CHANGES TO VIEW via TempData
                if (changes.Count > 0)
                {
                    TempData["ProfileChanges"] = System.Text.Json.JsonSerializer.Serialize(changes);
                }

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                TempData["ErrorMessage"] = "Unable to update profile. Please try again.";
                return View(model);
            }
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