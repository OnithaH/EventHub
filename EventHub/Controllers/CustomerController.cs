using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Models.ViewModels; // Add this line
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IEventService _eventService;
        private readonly IBookingService _bookingService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ApplicationDbContext context,
            IUserService userService,
            IEventService eventService,
            IBookingService bookingService,
            ILogger<CustomerController> logger)
        {
            _context = context;
            _userService = userService;
            _eventService = eventService;
            _bookingService = bookingService;
            _logger = logger;
        }

        /// <summary>
        /// Customer Dashboard - Main page for customers
        /// GET: /Customer/Dashboard
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Check if user is logged in and is a customer
                var userIdString = HttpContext.Session.GetString("UserId");
                var userRole = HttpContext.Session.GetString("UserRole");

                if (string.IsNullOrEmpty(userIdString) || userRole != "Customer")
                {
                    TempData["ErrorMessage"] = "Please log in as a customer to access the dashboard.";
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdString);

                // Get customer data
                var customer = await _userService.GetUserByIdAsync(userId);
                if (customer == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Build dashboard view model
                var viewModel = new CustomerDashboardViewModel
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    Email = customer.Email,
                    LoyaltyPoints = customer.LoyaltyPoints,

                    // Get statistics
                    UpcomingEventsCount = await GetUpcomingEventsCountAsync(userId),
                    TotalTicketsPurchased = await GetTotalTicketsAsync(userId),
                    TotalAmountSpent = await GetTotalAmountSpentAsync(userId),

                    // Get data collections - will be populated with real data later
                    UpcomingEvents = await GetUpcomingEventsAsync(userId),
                    RecentBookings = await GetRecentBookingsAsync(userId),
                    RecommendedEvents = new List<RecommendedEventDto>() // Placeholder for now
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer dashboard for user {UserId}", GetCurrentUserId());
                TempData["ErrorMessage"] = "Unable to load dashboard. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Get current user ID from session
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get upcoming events count for customer
        /// </summary>
        private async Task<int> GetUpcomingEventsCountAsync(int customerId)
        {
            try
            {
                return await _context.Bookings
                    .Where(b => b.CustomerId == customerId &&
                               b.Event.EventDate > DateTime.Now &&
                               b.Status == BookingStatus.Confirmed)
                    .CountAsync();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get total tickets purchased by customer
        /// </summary>
        private async Task<int> GetTotalTicketsAsync(int customerId)
        {
            try
            {
                var result = await _context.Bookings
                    .Where(b => b.CustomerId == customerId)
                    .SumAsync(b => (int?)b.Quantity);
                return result ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get total amount spent by customer
        /// </summary>
        private async Task<decimal> GetTotalAmountSpentAsync(int customerId)
        {
            try
            {
                var result = await _context.Bookings
                    .Where(b => b.CustomerId == customerId &&
                               (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed))
                    .SumAsync(b => (decimal?)b.TotalAmount);
                return result ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get upcoming events for customer
        /// </summary>
        private async Task<List<UpcomingEventDto>> GetUpcomingEventsAsync(int customerId)
        {
            try
            {
                var upcomingBookings = await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Where(b => b.CustomerId == customerId &&
                               b.Event.EventDate > DateTime.Now &&
                               b.Status == BookingStatus.Confirmed)
                    .OrderBy(b => b.Event.EventDate)
                    .Take(6)
                    .ToListAsync();

                return upcomingBookings.Select(b => new UpcomingEventDto
                {
                    EventId = b.Event.Id,
                    Title = b.Event.Title,
                    EventDate = b.Event.EventDate,
                    Category = b.Event.Category,
                    VenueName = b.Event.Venue?.Name ?? "TBA",
                    VenueLocation = b.Event.Venue?.Location ?? "TBA",
                    TicketPrice = b.Event.TicketPrice,
                    ImageUrl = b.Event.ImageUrl,
                    BookingId = b.Id,
                    TicketQuantity = b.Quantity,
                    BookingStatus = b.Status.ToString()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming events for customer {CustomerId}", customerId);
                return new List<UpcomingEventDto>();
            }
        }

        /// <summary>
        /// Get recent bookings for customer
        /// </summary>
        private async Task<List<RecentBookingDto>> GetRecentBookingsAsync(int customerId)
        {
            try
            {
                var recentBookings = await _context.Bookings
                    .Include(b => b.Event)
                    .Where(b => b.CustomerId == customerId)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .ToListAsync();

                return recentBookings.Select(b => new RecentBookingDto
                {
                    BookingId = b.Id,
                    EventId = b.Event.Id,
                    EventTitle = b.Event.Title,
                    EventCategory = b.Event.Category,
                    BookingDate = b.BookingDate,
                    EventDate = b.Event.EventDate,
                    Quantity = b.Quantity,
                    TotalAmount = b.TotalAmount,
                    Status = b.Status.ToString(),
                    EventImageUrl = b.Event.ImageUrl
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent bookings for customer {CustomerId}", customerId);
                return new List<RecentBookingDto>();
            }
        }

        #endregion
    }
}