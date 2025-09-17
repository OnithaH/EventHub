using EventHub.Data;
using EventHub.Services.Interfaces;
using EventHub.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using EventHub.Models;

namespace EventHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEventService _eventService;
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            IEventService eventService,
            IUserService userService,
            ApplicationDbContext context)
        {
            _logger = logger;
            _eventService = eventService;
            _userService = userService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get featured events with venue information (limit to 6)
                var featuredEvents = await _context.Events
                    .Include(e => e.Venue)
                    .Include(e => e.Organizer)
                    .Where(e => e.IsActive && e.EventDate > DateTime.Now)
                    .OrderBy(e => e.EventDate)
                    .Take(6)
                    .Select(e => new {
                        EventID = e.Id,
                        Title = e.Title,
                        Category = e.Category,
                        Date = e.EventDate,
                        Time = e.EventTime,
                        TicketPrice = e.TicketPrice,
                        AvailableTickets = e.AvailableTickets,
                        ImageUrl = e.ImageUrl,
                        VenueName = e.Venue.Name,
                        Location = e.Venue.Location
                    })
                    .ToListAsync();

                ViewBag.FeaturedEvents = featuredEvents;

                // Get statistics for hero section
                ViewBag.TotalEvents = await _context.Events.CountAsync(e => e.IsActive && e.EventDate > DateTime.Now);
                ViewBag.TotalUsers = await _context.Users.CountAsync(u => u.IsActive);
                ViewBag.TotalBookings = await _context.Bookings.CountAsync();
                ViewBag.TotalOrganizers = await _context.Users.CountAsync(u => u.Role == UserRole.Organizer && u.IsActive);

                _logger.LogInformation("Homepage loaded successfully with {EventCount} featured events", featuredEvents.Count());
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading homepage data");

                // Fallback - still show page but with empty data
                ViewBag.FeaturedEvents = new List<object>();
                ViewBag.TotalEvents = 0;
                ViewBag.TotalUsers = 0;
                ViewBag.TotalBookings = 0;
                ViewBag.TotalOrganizers = 0;

                return View();
            }
        }

        // Newsletter subscription endpoint
        [HttpPost]
        public async Task<IActionResult> Subscribe(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
                {
                    TempData["ErrorMessage"] = "Please enter a valid email address.";
                    return RedirectToAction("Index");
                }

                // Here you would implement newsletter subscription logic
                // For now, we'll just log it
                _logger.LogInformation("Newsletter subscription request for email: {Email}", email);

                TempData["SuccessMessage"] = "Thank you for subscribing! You'll receive updates about amazing events.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing newsletter subscription");
                TempData["ErrorMessage"] = "An error occurred. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Helper method to validate email
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
    }
}