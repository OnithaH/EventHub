using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Models.ViewModels;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;
        private readonly IUserService _userService;
        private readonly IQRCodeService _qrCodeService;
        private readonly ILogger<BookingController> _logger;

        public BookingController(
            ApplicationDbContext context,
            IBookingService bookingService,
            IEventService eventService,
            IUserService userService,
            IQRCodeService qrCodeService,
            ILogger<BookingController> logger)
        {
            _context = context;
            _bookingService = bookingService;
            _eventService = eventService;
            _userService = userService;
            _qrCodeService = qrCodeService;
            _logger = logger;
        }

        /// <summary>
        /// Display all bookings for the logged-in customer
        /// GET: /Booking/MyBookings
        /// </summary>
        public async Task<IActionResult> MyBookings(
            string searchTerm = "",
            string statusFilter = "",
            string dateFilter = "",
            string sortBy = "date-desc",
            int page = 1)
        {
            try
            {
                // Check authentication
                var userIdString = HttpContext.Session.GetString("UserId");
                var userRole = HttpContext.Session.GetString("UserRole");

                if (string.IsNullOrEmpty(userIdString) || userRole != "Customer")
                {
                    TempData["ErrorMessage"] = "Please log in as a customer to view your bookings.";
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                // Base query
                var query = _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.Payment)
                    .Include(b => b.Tickets)
                    .Where(b => b.CustomerId == customerId);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(b =>
                        b.Event.Title.Contains(searchTerm) ||
                        b.BookingReference.Contains(searchTerm));
                }

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    if (Enum.TryParse<BookingStatus>(statusFilter, out var status))
                    {
                        query = query.Where(b => b.Status == status);
                    }
                }

                // Apply date filter
                var now = DateTime.UtcNow;
                query = dateFilter switch
                {
                    "upcoming" => query.Where(b => b.Event.EventDate > now),
                    "past" => query.Where(b => b.Event.EventDate <= now),
                    "today" => query.Where(b => b.Event.EventDate.Date == now.Date),
                    "week" => query.Where(b => b.Event.EventDate >= now &&
                                              b.Event.EventDate <= now.AddDays(7)),
                    "month" => query.Where(b => b.Event.EventDate >= now &&
                                               b.Event.EventDate <= now.AddMonths(1)),
                    _ => query
                };

                // Apply sorting
                query = sortBy switch
                {
                    "date-asc" => query.OrderBy(b => b.BookingDate),
                    "date-desc" => query.OrderByDescending(b => b.BookingDate),
                    "amount-asc" => query.OrderBy(b => b.TotalAmount),
                    "amount-desc" => query.OrderByDescending(b => b.TotalAmount),
                    _ => query.OrderByDescending(b => b.BookingDate)
                };

                // Get statistics
                var allBookings = await _context.Bookings
                    .Where(b => b.CustomerId == customerId)
                    .Include(b => b.Event)
                    .ToListAsync();

                var totalBookings = allBookings.Count;
                var upcomingCount = allBookings.Count(b => b.Event.EventDate > now &&
                                                          b.Status == BookingStatus.Confirmed);
                var completedCount = allBookings.Count(b => b.Status == BookingStatus.Completed);

                // Pagination
                var pageSize = 10;
                var totalRecords = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                var bookings = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new BookingDisplayDto
                    {
                        Id = b.Id,
                        BookingReference = b.BookingReference ?? $"BK{b.Id:D6}",
                        BookingDate = b.BookingDate,
                        Quantity = b.Quantity,
                        TotalAmount = b.TotalAmount,
                        Status = b.Status,
                        EventId = b.Event.Id,
                        EventTitle = b.Event.Title,
                        EventDate = b.Event.EventDate,
                        EventCategory = b.Event.Category,
                        EventImageUrl = b.Event.ImageUrl,
                        VenueName = b.Event.Venue.Name,
                        VenueLocation = b.Event.Venue.Location,
                        PaymentStatus = b.Payment != null ? b.Payment.Status.ToString() : "Pending",
                        PaymentMethod = b.Payment != null ? b.Payment.PaymentMethod : null,
                        TicketCount = b.Tickets.Count,
                        HasTickets = b.Tickets.Any()
                    })
                    .ToListAsync();

                var viewModel = new MyBookingsViewModel
                {
                    Bookings = bookings,
                    TotalBookings = totalBookings,
                    UpcomingCount = upcomingCount,
                    CompletedCount = completedCount,
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    DateFilter = dateFilter,
                    SortBy = sortBy,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalRecords = totalRecords
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bookings for customer");
                TempData["ErrorMessage"] = "Unable to load your bookings. Please try again.";
                return RedirectToAction("Dashboard", "Customer");
            }
        }

        /// <summary>
        /// View booking details
        /// GET: /Booking/Details/5
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                var booking = await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.Payment)
                    .Include(b => b.Tickets)
                    .Include(b => b.Customer)
                    .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customerId);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("MyBookings");
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking details {BookingId}", id);
                TempData["ErrorMessage"] = "Unable to load booking details.";
                return RedirectToAction("MyBookings");
            }
        }

        /// <summary>
        /// Cancel a booking
        /// POST: /Booking/Cancel/5
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Json(new { success = false, message = "Please log in to cancel bookings." });
                }

                var customerId = int.Parse(userIdString);

                var booking = await _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Tickets)
                    .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customerId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found." });
                }

                // Check if booking can be cancelled (at least 7 days before event)
                if (booking.Event.EventDate <= DateTime.UtcNow.AddDays(7))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bookings can only be cancelled at least 7 days before the event."
                    });
                }

                if (booking.Status != BookingStatus.Confirmed)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Only confirmed bookings can be cancelled."
                    });
                }

                // Update booking status
                booking.Status = BookingStatus.Cancelled;

                // Cancel all tickets
                foreach (var ticket in booking.Tickets)
                {
                    ticket.Status = TicketStatus.Cancelled;
                }

                // Restore available tickets
                booking.Event.AvailableTickets += booking.Quantity;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Booking {BookingId} cancelled by customer {CustomerId}",
                    id, customerId);

                return Json(new
                {
                    success = true,
                    message = "Booking cancelled successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
                return Json(new
                {
                    success = false,
                    message = "An error occurred while cancelling the booking."
                });
            }
        }

        /// <summary>
        /// Create booking - Display booking form
        /// GET: /Booking/Create/5
        /// </summary>
        public async Task<IActionResult> Create(int id)
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Please log in to book tickets.";
                return RedirectToAction("Login", "Account");
            }

            var eventModel = await _eventService.GetEventByIdAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            if (eventModel.AvailableTickets <= 0)
            {
                TempData["ErrorMessage"] = "Sorry, this event is sold out.";
                return RedirectToAction("Details", "Event", new { id });
            }

            var viewModel = new BookingViewModel
            {
                EventId = eventModel.Id,
                EventTitle = eventModel.Title,
                VenueName = eventModel.Venue.Name,
                EventDate = eventModel.EventDate,
                TicketPrice = eventModel.TicketPrice,
                AvailableTickets = eventModel.AvailableTickets,
                Quantity = 1
            };

            return View(viewModel);
        }

        /// <summary>
        /// Process booking creation
        /// POST: /Booking/Create
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                var eventModel = await _eventService.GetEventByIdAsync(model.EventId);
                if (eventModel == null || eventModel.AvailableTickets < model.Quantity)
                {
                    ModelState.AddModelError("", "Insufficient tickets available.");
                    return View(model);
                }

                try
                {
                    // Calculate total amount
                    var totalAmount = await _bookingService.CalculateTotalAmountAsync(
                        model.EventId, model.Quantity);

                    // Create booking
                    var booking = new Booking
                    {
                        CustomerId = int.Parse(userIdString),
                        EventId = model.EventId,
                        Quantity = model.Quantity,
                        TotalAmount = totalAmount,
                        Status = BookingStatus.Pending
                    };

                    var createdBooking = await _bookingService.CreateBookingAsync(booking);

                    // Redirect to checkout/payment
                    return RedirectToAction("Checkout", new { id = createdBooking.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating booking");
                    ModelState.AddModelError("", "An error occurred while creating your booking.");
                }
            }

            return View(model);
        }

        /// <summary>
        /// Display checkout page for a booking
        /// GET: /Booking/Checkout/5
        /// </summary>
        public async Task<IActionResult> Checkout(int id)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    TempData["ErrorMessage"] = "Please log in to complete checkout.";
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                // Get booking details
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.Customer)
                    .FirstOrDefaultAsync(b => b.Id == id &&
                                            b.CustomerId == customerId);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("MyBookings");
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    TempData["ErrorMessage"] = "This booking has already been processed.";
                    return RedirectToAction("Details", new { id });
                }

                // Calculate amounts
                var subtotal = booking.TotalAmount;
                var serviceFee = subtotal * 0.05m; // 5% service fee
                var totalAmount = subtotal + serviceFee;

                // Get customer info
                var customer = booking.Customer;

                var viewModel = new CheckoutViewModel
                {
                    BookingId = booking.Id,
                    EventId = booking.Event.Id,
                    EventTitle = booking.Event.Title,
                    EventDate = booking.Event.EventDate,
// Add this method to your BookingController.cs

                    /// <summary>
                    /// Display checkout page for a booking
                    /// GET: /Booking/Checkout/5
                    /// </summary>
public async Task<IActionResult> Checkout(int id)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    TempData["ErrorMessage"] = "Please log in to complete checkout.";
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                // Get booking details
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.Customer)
                    .FirstOrDefaultAsync(b => b.Id == id &&
                                            b.CustomerId == customerId);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("MyBookings");
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    TempData["ErrorMessage"] = "This booking has already been processed.";
                    return RedirectToAction("Details", new { id });
                }

                // Calculate amounts
                var subtotal = booking.TotalAmount;
                var serviceFee = subtotal * 0.05m; // 5% service fee
                var totalAmount = subtotal + serviceFee;

                // Get customer info
                var customer = booking.Customer;

                var viewModel = new CheckoutViewModel
                {
                    BookingId = booking.Id,
                    EventId = booking.Event.Id,
                    EventTitle = booking.Event.Title,
                    EventDate = booking.Event.EventDate,
                    VenueName = booking.Event.Venue.Name,
                    EventImageUrl = booking.Event.ImageUrl,
                    TicketPrice = booking.Event.TicketPrice,
                    Quantity = booking.Quantity,
                    Subtotal = subtotal,
                    ServiceFee = serviceFee,
                    DiscountAmount = 0,
                    LoyaltyPointsDiscount = 0,
                    Amount = totalAmount,
                    FirstName = customer.Name.Split(' ').FirstOrDefault() ?? customer.Name,
                    LastName = customer.Name.Split(' ').LastOrDefault() ?? "",
                    Email = customer.Email,
                    Phone = customer.Phone ?? "",
                    AvailableLoyaltyPoints = customer.LoyaltyPoints,
                    LoyaltyPointsUsed = 0,
                    PointsToEarn = (int)totalAmount // 1 point per dollar
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page for booking {BookingId}", id);
                TempData["ErrorMessage"] = "Unable to load checkout page.";
                return RedirectToAction("MyBookings");
            }
        }
    }
}