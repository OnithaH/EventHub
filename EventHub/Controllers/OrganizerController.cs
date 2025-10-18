using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Services.Implementations;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEventService _eventService;
        private readonly IUserService _userService;
        private readonly IQRCodeService _qrCodeService;
        private readonly ILogger<OrganizerController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IBlobStorageService _blobStorageService;

        public OrganizerController(
            ApplicationDbContext context,
            IEventService eventService,
            IUserService userService,
            IQRCodeService qrCodeService,
            ILogger<OrganizerController> logger,
            IWebHostEnvironment webHostEnvironment,
            IBlobStorageService blobStorageService)
        {
            _context = context;
            _eventService = eventService;
            _userService = userService;
            _qrCodeService = qrCodeService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _blobStorageService = blobStorageService;
        }

        #region Authorization Helper
        private bool IsOrganizer()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Organizer" || userRole == "Admin";
        }

        private int GetCurrentOrganizerId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out var userId) ? userId : 0;
        }
        #endregion

        #region Dashboard
        /// <summary>
        /// Organizer Dashboard - Overview of all events and statistics
        /// GET: /Organizer/Dashboard
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            if (!IsOrganizer())
            {
                TempData["ErrorMessage"] = "Access denied. Organizer role required.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var organizerId = GetCurrentOrganizerId();
                var organizer = await _context.Users.FindAsync(organizerId);

                if (organizer == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get organizer statistics
                var totalEvents = await _context.Events
                    .CountAsync(e => e.OrganizerId == organizerId);

                var activeEvents = await _context.Events
                    .CountAsync(e => e.OrganizerId == organizerId && e.IsActive && e.EventDate > DateTime.UtcNow);

                var totalTicketsSold = await _context.Bookings
                    .Where(b => b.Event.OrganizerId == organizerId &&
                               (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed))
                    .SumAsync(b => (int?)b.Quantity) ?? 0;

                var totalRevenue = await _context.Bookings
                    .Where(b => b.Event.OrganizerId == organizerId &&
                               (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed))
                    .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

                var totalCustomers = await _context.Bookings
                    .Where(b => b.Event.OrganizerId == organizerId)
                    .Select(b => b.CustomerId)
                    .Distinct()
                    .CountAsync();

                // Get recent events with stats
                var recentEvents = await _context.Events
                    .Include(e => e.Venue)
                    .Where(e => e.OrganizerId == organizerId && e.IsActive)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(5)
                    .Select(e => new
                    {
                        EventId = e.Id,
                        Title = e.Title,
                        Date = e.EventDate,
                        Venue = e.Venue.Name,
                        Category = e.Category,
                        ImageUrl = e.ImageUrl,
                        TicketsSold = e.TotalTickets - e.AvailableTickets,
                        Capacity = e.TotalTickets,
                        Revenue = _context.Bookings
                            .Where(b => b.EventId == e.Id &&
                                   (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed))
                            .Sum(b => (decimal?)b.TotalAmount) ?? 0
                    })
                    .ToListAsync();

                // Get recent bookings for organizer's events
                var recentBookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Event)
                    .Where(b => b.Event.OrganizerId == organizerId)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .Select(b => new
                    {
                        BookingId = b.Id,
                        BookingReference = b.BookingReference,
                        CustomerName = b.Customer.Name,
                        CustomerEmail = b.Customer.Email,
                        EventTitle = b.Event.Title,
                        Quantity = b.Quantity,
                        TotalAmount = b.TotalAmount,
                        BookingDate = b.BookingDate,
                        Status = b.Status
                    })
                    .ToListAsync();

                // Monthly revenue data (last 6 months)
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                var monthlyRevenue = await _context.Bookings
                    .Where(b => b.Event.OrganizerId == organizerId &&
                               b.BookingDate >= sixMonthsAgo &&
                               (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed))
                    .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(b => b.TotalAmount),
                        Tickets = g.Sum(b => b.Quantity)
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToListAsync();

                // Pass data to view
                ViewBag.OrganizerName = organizer.Name;
                ViewBag.CompanyName = organizer.Company;
                ViewBag.TotalEvents = totalEvents;
                ViewBag.ActiveEvents = activeEvents;
                ViewBag.TotalTicketsSold = totalTicketsSold;
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalCustomers = totalCustomers;
                ViewBag.RecentEvents = recentEvents;
                ViewBag.RecentBookings = recentBookings;
                ViewBag.MonthlyRevenue = monthlyRevenue;

                // Calculate revenue growth (compare last 2 months)
                if (monthlyRevenue.Count >= 2)
                {
                    var lastMonth = monthlyRevenue[monthlyRevenue.Count - 1];
                    var previousMonth = monthlyRevenue[monthlyRevenue.Count - 2];
                    var growth = previousMonth.Revenue > 0
                        ? ((lastMonth.Revenue - previousMonth.Revenue) / previousMonth.Revenue) * 100
                        : 0;
                    ViewBag.RevenueGrowth = Math.Round(growth, 1);
                }
                else
                {
                    ViewBag.RevenueGrowth = 0;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading organizer dashboard");
                TempData["ErrorMessage"] = "An error occurred loading the dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }
        #endregion

        #region Create Event
        /// <summary>
        /// Show Create Event Form
        /// GET: /Organizer/Create
        /// </summary>
        public async Task<IActionResult> Create()
        {
            if (!IsOrganizer())
            {
                TempData["ErrorMessage"] = "Access denied. Organizer role required.";
                return RedirectToAction("Login", "Account");
            }

            // Load venues for dropdown
            ViewBag.Venues = await _context.Venues
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name, v.Location, v.Capacity })
                .ToListAsync();

            return View();
        }

        /// <summary>
        /// Create New Event
        /// POST: /Organizer/Create
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event eventModel, IFormFile? eventImage,
            string? NewVenueName, string? NewVenueLocation, int? NewVenueCapacity, string? NewVenueAddress)
        {
            try
            {
                if (!IsOrganizer())
                {
                    return Forbid();
                }

                var organizerId = GetCurrentOrganizerId();

                // Remove navigation properties from validation
                ModelState.Remove("Organizer");
                ModelState.Remove("Venue");

                // Remove VenueId validation if creating new venue
                if (!string.IsNullOrEmpty(NewVenueName))
                {
                    ModelState.Remove("VenueId");
                }

                // Convert EventDate to UTC
                if (eventModel.EventDate.Kind == DateTimeKind.Unspecified)
                {
                    eventModel.EventDate = DateTime.SpecifyKind(eventModel.EventDate, DateTimeKind.Utc);
                }

                // Handle new venue creation
                if (!string.IsNullOrEmpty(NewVenueName))
                {
                    // Validate new venue fields
                    if (string.IsNullOrEmpty(NewVenueLocation))
                    {
                        ModelState.AddModelError("NewVenueLocation", "Location is required");
                    }
                    if (!NewVenueCapacity.HasValue || NewVenueCapacity <= 0)
                    {
                        ModelState.AddModelError("NewVenueCapacity", "Capacity must be greater than 0");
                    }

                    if (!ModelState.IsValid)
                    {
                        ViewBag.Venues = await _context.Venues
                            .OrderBy(v => v.Name)
                            .Select(v => new { v.Id, v.Name, v.Location, v.Capacity })
                            .ToListAsync();
                        return View(eventModel);
                    }

                    // Create new venue
                    var newVenue = new Venue
                    {
                        Name = NewVenueName,
                        Location = NewVenueLocation!,
                        Capacity = NewVenueCapacity!.Value,
                        Address = NewVenueAddress,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Venues.Add(newVenue);
                    await _context.SaveChangesAsync();

                    eventModel.VenueId = newVenue.Id;
                }
                else if (eventModel.VenueId <= 0)
                {
                    ModelState.AddModelError("VenueId", "Please select a venue or create a new one");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Venues = await _context.Venues
                        .OrderBy(v => v.Name)
                        .Select(v => new { v.Id, v.Name, v.Location, v.Capacity })
                        .ToListAsync();
                    return View(eventModel);
                }

                if (eventImage != null && eventImage.Length > 0)
                {
                    try
                    {
                        // Upload new image to Blob Storage
                        eventModel.ImageUrl = await _blobStorageService.UploadImageAsync(eventImage);
                        _logger.LogInformation($"✅ Event image updated to: {eventModel.ImageUrl}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"❌ Image upload failed: {ex.Message}");
                        TempData["ErrorMessage"] = "Failed to upload image. Please try again.";
                        return RedirectToAction(nameof(MyEvents));
                    }
                }
                else if (string.IsNullOrEmpty(eventModel.ImageUrl))
                {
                    eventModel.ImageUrl = "https://eventhubstorageonitha.blob.core.windows.net/eventhub-images/default-event.jpg";
                }


                // Set event properties
                eventModel.OrganizerId = organizerId;
                eventModel.CreatedAt = DateTime.UtcNow;
                eventModel.TotalTickets = eventModel.AvailableTickets;

                await _eventService.CreateEventAsync(eventModel);

                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction(nameof(MyEvents));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                ModelState.AddModelError("", "An error occurred while creating the event.");

                ViewBag.Venues = await _context.Venues
                    .OrderBy(v => v.Name)
                    .Select(v => new { v.Id, v.Name, v.Location, v.Capacity })
                    .ToListAsync();

                return View(eventModel);
            }
        }
        #endregion

        #region Edit Event
        /// <summary>
        /// Show Edit Event Form
        /// GET: /Organizer/Edit/5
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsOrganizer())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            var eventModel = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventModel == null)
            {
                return NotFound();
            }

            var organizerId = GetCurrentOrganizerId();
            if (eventModel.OrganizerId != organizerId && HttpContext.Session.GetString("UserRole") != "Admin")
            {
                TempData["ErrorMessage"] = "You can only edit your own events.";
                return RedirectToAction(nameof(MyEvents));
            }

            // Load venues for dropdown
            ViewBag.Venues = await _context.Venues
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name, v.Location, v.Capacity })
                .ToListAsync();

            // Get booking count for this event
            ViewBag.BookingCount = await _context.Bookings
                .CountAsync(b => b.EventId == id);

            return View(eventModel);
        }

        /// <summary>
        /// Update Event
        /// POST: /Organizer/Edit/5
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event eventModel, IFormFile? eventImage)
        {
            if (id != eventModel.Id)
            {
                return BadRequest();
            }

            if (!IsOrganizer())
            {
                return Forbid();
            }

            var organizerId = GetCurrentOrganizerId();
            var existingEvent = await _context.Events.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            if (existingEvent.OrganizerId != organizerId &&
                HttpContext.Session.GetString("UserRole") != "Admin")
            {
                TempData["ErrorMessage"] = "You can only edit your own events.";
                return RedirectToAction(nameof(MyEvents));
            }

            try
            {
                // 🔧 FIX: Calculate how many tickets have been sold
                int ticketsSold = existingEvent.TotalTickets - existingEvent.AvailableTickets;

                // 🔧 FIX: When updating TotalTickets, recalculate AvailableTickets
                // AvailableTickets = NewTotalTickets - TicketsSold
                eventModel.AvailableTickets = eventModel.TotalTickets - ticketsSold;

                // Debug logging
                _logger.LogInformation("📊 EVENT TICKETS UPDATE DEBUG:");
                _logger.LogInformation("   Old Total: {OldTotal}, Old Available: {OldAvailable}",
                    existingEvent.TotalTickets, existingEvent.AvailableTickets);
                _logger.LogInformation("   Tickets Sold: {Sold}", ticketsSold);
                _logger.LogInformation("   New Total: {NewTotal}, New Available: {NewAvailable}",
                    eventModel.TotalTickets, eventModel.AvailableTickets);

                // Handle image upload to Azure Blob Storage
                if (eventImage != null && eventImage.Length > 0)
                {
                    try
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingEvent.ImageUrl) &&
                            !existingEvent.ImageUrl.Contains("default-event"))
                        {
                            await _blobStorageService.DeleteImageAsync(existingEvent.ImageUrl);
                        }

                        // Upload new image to Blob Storage
                        eventModel.ImageUrl = await _blobStorageService.UploadImageAsync(eventImage);
                        _logger.LogInformation($"✅ Event image updated to: {eventModel.ImageUrl}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"❌ Image upload failed: {ex.Message}");
                        TempData["ErrorMessage"] = "Failed to upload image. Please try again.";
                        return RedirectToAction(nameof(MyEvents));
                    }
                }
                else if (string.IsNullOrEmpty(eventModel.ImageUrl))
                {
                    eventModel.ImageUrl = "https://eventhubstorageonitha.blob.core.windows.net/eventhub-images/default-event.jpg";
                }

                // Update the event
                _context.Events.Update(eventModel);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Event {EventId} updated successfully", id);
                TempData["SuccessMessage"] = "Event updated successfully!";
                return RedirectToAction(nameof(MyEvents));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", id);
                ModelState.AddModelError("", "An error occurred while updating the event.");

                ViewBag.Venues = await _context.Venues
                    .OrderBy(v => v.Name)
                    .Select(v => new { v.Id, v.Name, v.Location, v.Capacity })
                    .ToListAsync();

                ViewBag.BookingCount = await _context.Bookings
                    .CountAsync(b => b.EventId == id);

                return View(eventModel);
            }
        }
        #endregion

        #region My Events
        /// <summary>
        /// List all events by current organizer
        /// GET: /Organizer/MyEvents
        /// </summary>
        public async Task<IActionResult> MyEvents()
        {
            if (!IsOrganizer())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var organizerId = GetCurrentOrganizerId();

                var events = await _context.Events
                    .Include(e => e.Venue)
                    .Where(e => e.OrganizerId == organizerId)
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => new
                    {
                        EventId = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        Category = e.Category,
                        EventDate = e.EventDate,
                        EventTime = e.EventTime,
                        VenueName = e.Venue.Name,
                        Location = e.Venue.Location,
                        TicketPrice = e.TicketPrice,
                        AvailableTickets = e.AvailableTickets,
                        TotalTickets = e.TotalTickets,
                        ImageUrl = e.ImageUrl,
                        IsActive = e.IsActive,
                        CreatedAt = e.CreatedAt,
                        TicketsSold = e.TotalTickets - e.AvailableTickets,
                        Revenue = _context.Bookings
                            .Where(b => b.EventId == e.Id &&
                                   (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed))
                            .Sum(b => (decimal?)b.TotalAmount) ?? 0,
                        Status = e.EventDate < DateTime.UtcNow ? "completed" :
                                (e.IsActive ? "active" : "draft")
                    })
                    .ToListAsync();

                // Calculate quick stats
                ViewBag.TotalEvents = events.Count;
                ViewBag.ActiveEvents = events.Count(e => e.Status == "active");
                ViewBag.TotalTicketsSold = events.Sum(e => e.TicketsSold);
                ViewBag.TotalRevenue = events.Sum(e => e.Revenue);

                ViewBag.Events = events;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading my events");
                TempData["ErrorMessage"] = "An error occurred loading your events.";
                return RedirectToAction("Dashboard");
            }
        }
        #endregion

        #region Delete Event
        /// <summary>
        /// Delete Event
        /// POST: /Organizer/Delete/5
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsOrganizer())
            {
                return Forbid();
            }

            try
            {
                var eventModel = await _context.Events.FindAsync(id);

                if (eventModel == null)
                {
                    TempData["ErrorMessage"] = "Event not found.";
                    return RedirectToAction(nameof(MyEvents));
                }

                var organizerId = GetCurrentOrganizerId();
                if (eventModel.OrganizerId != organizerId && HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    return Forbid();
                }

                // Check if event has ANY confirmed bookings
                var hasConfirmedBookings = await _context.Bookings
                    .AnyAsync(b => b.EventId == id && b.Status == BookingStatus.Confirmed);

                if (hasConfirmedBookings)
                {
                    var bookingCount = await _context.Bookings
                        .CountAsync(b => b.EventId == id && b.Status == BookingStatus.Confirmed);

                    TempData["ErrorMessage"] = $"Cannot delete event with {bookingCount} confirmed booking(s). Please contact customers first or cancel the event instead.";
                    return RedirectToAction(nameof(MyEvents));
                }

                // Delete event image if exists
                if (!string.IsNullOrEmpty(eventModel.ImageUrl) &&
                    eventModel.ImageUrl != "/images/events/default-event.jpg")
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        eventModel.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                        _logger.LogInformation("Deleted event image: {ImagePath}", imagePath);
                    }
                }

                // Delete the event
                _context.Events.Remove(eventModel);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Event {EventId} deleted by organizer {OrganizerId}", id, organizerId);
                TempData["SuccessMessage"] = "Event deleted successfully!";
                return RedirectToAction(nameof(MyEvents));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the event. Please try again.";
                return RedirectToAction(nameof(MyEvents));
            }
        }
        #endregion

        #region QR Scanner
        /// <summary>
        /// GET: QR Scanner Page
        /// </summary>
        public IActionResult QRScanner()
        {
            if (!IsOrganizer())
            {
                TempData["ErrorMessage"] = "Access denied. Only organizers can access the QR scanner.";
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        /// <summary>
        /// POST: Verify Ticket via QR Code
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> VerifyTicket([FromBody] VerifyTicketRequest request)
        {
            try
            {
                _logger.LogInformation("🔍 Verifying ticket: {TicketCode}", request.QRCodeData);

                // Simple lookup by ticket number (what's in the QR code)
                var ticketCode = request.QRCodeData.Trim();

                // Fetch ticket with related data
                var ticket = await _context.Tickets
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Customer)
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Event)
                            .ThenInclude(e => e.Venue)
                    .FirstOrDefaultAsync(t => t.TicketNumber == ticketCode);

                if (ticket == null)
                {
                    _logger.LogWarning("❌ Ticket not found: {TicketCode}", ticketCode);
                    return Json(new
                    {
                        success = false,
                        status = "invalid",
                        message = "Ticket not found in system"
                    });
                }

                _logger.LogInformation("✅ Ticket found: {TicketNumber}, Status: {Status}",
                    ticket.TicketNumber, ticket.Status);

                // Check if ticket already used
                if (ticket.Status == TicketStatus.Used)
                {
                    _logger.LogWarning("⚠️ Ticket already used: {TicketNumber}", ticket.TicketNumber);
                    return Json(new
                    {
                        success = false,
                        status = "used",
                        message = "This ticket has already been used",
                        ticketData = new
                        {
                            ticketId = ticket.TicketNumber,
                            customerName = ticket.Booking.Customer.Name,
                            eventTitle = ticket.Booking.Event.Title,
                            usedDate = ticket.UsedDate?.ToString("MMM dd, yyyy hh:mm tt")
                        }
                    });
                }

                // Check if ticket is valid (use Active enum value)
                if (ticket.Status != TicketStatus.Active)
                {
                    _logger.LogWarning("❌ Ticket not active: {TicketNumber}, Status: {Status}",
                        ticket.TicketNumber, ticket.Status);
                    return Json(new
                    {
                        success = false,
                        status = "expired",
                        message = "Ticket is not valid",
                        ticketData = new
                        {
                            ticketId = ticket.TicketNumber,
                            status = ticket.Status.ToString()
                        }
                    });
                }

                // Check if event date has passed
                if (ticket.Booking.Event.EventDate.Date < DateTime.UtcNow.Date)
                {
                    _logger.LogWarning("❌ Event date passed: {EventDate}",
                        ticket.Booking.Event.EventDate);
                    return Json(new
                    {
                        success = false,
                        status = "expired",
                        message = "Event date has passed",
                        ticketData = new
                        {
                            eventDate = ticket.Booking.Event.EventDate.ToString("MMM dd, yyyy")
                        }
                    });
                }

                // Mark ticket as used
                _logger.LogInformation("✅ Marking ticket as USED: {TicketNumber}", ticket.TicketNumber);
                ticket.Status = TicketStatus.Used;
                ticket.UsedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("🎉 Ticket verified successfully: {TicketNumber}", ticket.TicketNumber);

                // Return success
                return Json(new
                {
                    success = true,
                    status = "valid",
                    message = "Valid ticket - Entry allowed",
                    ticketData = new
                    {
                        ticketId = ticket.TicketNumber,
                        ticketType = ticket.SeatNumber ?? "General Admission",
                        customerName = ticket.Booking.Customer.Name,
                        customerEmail = ticket.Booking.Customer.Email,
                        eventTitle = ticket.Booking.Event.Title,
                        eventDate = ticket.Booking.Event.EventDate.ToString("MMM dd, yyyy"),
                        venue = ticket.Booking.Event.Venue.Name,
                        price = $"Rs. {ticket.Booking.TotalAmount / ticket.Booking.Quantity:N0}",
                        purchaseDate = ticket.Booking.BookingDate.ToString("MMM dd, yyyy"),
                        verificationTime = DateTime.UtcNow.ToString("hh:mm:ss tt")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error verifying ticket: {TicketCode}", request.QRCodeData);
                return Json(new
                {
                    success = false,
                    status = "error",
                    message = "An error occurred while verifying the ticket"
                });
            }
        }

        /// <summary>
        /// GET: Ticket Verification Details Page
        /// </summary>
        public async Task<IActionResult> TicketVerification(string ticketCode)
        {
            if (!IsOrganizer())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(ticketCode))
            {
                TempData["ErrorMessage"] = "No ticket code provided";
                return RedirectToAction("QRScanner");
            }

            ViewBag.TicketCode = ticketCode;
            return View();
        }
        #endregion









        #region Sales Reports & Analytics

        /// <summary>
        /// GET: Sales Reports Page
        /// </summary>
        public async Task<IActionResult> SalesReports()
        {
            if (!IsOrganizer())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            var organizerId = GetCurrentOrganizerId();
            var events = await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            ViewBag.Events = events;
            return View();
        }

        /// <summary>
        /// POST: Get Sales Report Data (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetSalesReportData([FromBody] SalesReportRequest request)
        {
            try
            {
                if (!IsOrganizer())
                    return Unauthorized();

                var organizerId = GetCurrentOrganizerId();

                // Calculate date range
                var startDate = request.TimeFrame == "all"
                    ? DateTime.MinValue
                    : DateTime.UtcNow.AddDays(-int.Parse(request.TimeFrame));

                // Base query for bookings
                var bookingsQuery = _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Payment)
                    .Include(b => b.Customer)
                    .Where(b => b.Event.OrganizerId == organizerId && b.BookingDate >= startDate);

                // Filter by event if specified
                if (!string.IsNullOrEmpty(request.EventId) && int.TryParse(request.EventId, out int eventId))
                {
                    bookingsQuery = bookingsQuery.Where(b => b.EventId == eventId);
                }

                // Filter by payment status if specified
                if (!string.IsNullOrEmpty(request.PaymentStatus))
                {
                    var status = Enum.Parse<PaymentStatus>(request.PaymentStatus);
                    bookingsQuery = bookingsQuery.Where(b => b.Payment != null && b.Payment.Status == status);
                }

                var bookings = await bookingsQuery.ToListAsync();

                // Calculate metrics
                var metrics = new
                {
                    totalRevenue = bookings
                        .Where(b => b.Payment != null && b.Payment.Status == PaymentStatus.Completed)
                        .Sum(b => b.TotalAmount),
                    totalTickets = bookings.Sum(b => b.Quantity),
                    totalBookings = bookings.Count,
                    avgOrderValue = bookings.Count > 0
                        ? bookings.Average(b => b.TotalAmount)
                        : 0
                };

                // Generate charts data
                var revenueTrendDates = new List<string>();
                var revenueTrendAmounts = new List<decimal>();

                var currentDate = startDate;
                while (currentDate <= DateTime.UtcNow)
                {
                    var dayBookings = bookings
                        .Where(b => b.BookingDate.Date == currentDate.Date &&
                                   b.Payment != null &&
                                   b.Payment.Status == PaymentStatus.Completed)
                        .Sum(b => b.TotalAmount);

                    revenueTrendDates.Add(currentDate.ToString("MMM dd"));
                    revenueTrendAmounts.Add(dayBookings);
                    currentDate = currentDate.AddDays(1);
                }

                // Payment status breakdown
                var paymentStatusBreakdown = new[]
                {
            bookings.Count(b => b.Payment != null && b.Payment.Status == PaymentStatus.Completed),
            bookings.Count(b => b.Payment != null && b.Payment.Status == PaymentStatus.Pending),
            bookings.Count(b => b.Payment != null && b.Payment.Status == PaymentStatus.Failed)
        };

                // Top events by revenue
                var topEvents = bookings
                    .GroupBy(b => b.Event.Title)
                    .Select(g => new
                    {
                        name = g.Key,
                        revenue = g.Where(b => b.Payment != null && b.Payment.Status == PaymentStatus.Completed)
                            .Sum(b => b.TotalAmount)
                    })
                    .OrderByDescending(x => x.revenue)
                    .Take(5)
                    .ToList();

                // Ticket distribution by category
                var ticketDistribution = bookings
                    .GroupBy(b => b.Event.Category)
                    .Select(g => new
                    {
                        category = g.Key,
                        count = g.Sum(b => b.Quantity)
                    })
                    .ToList();

                // Sales list for table
                var sales = bookings
                    .Select(b => new
                    {
                        bookingId = b.Id,
                        bookingReference = b.BookingReference ?? $"BK{b.Id}",
                        eventTitle = b.Event.Title,
                        customerName = b.Customer.Name,
                        bookingDate = b.BookingDate,
                        quantity = b.Quantity,
                        totalAmount = b.TotalAmount,
                        discountAmount = 0m, // Calculate if you have discount logic
                        paymentStatus = b.Payment?.Status.ToString() ?? "No Payment"
                    })
                    .OrderByDescending(b => b.bookingDate)
                    .ToList();

                var charts = new
                {
                    revenueTrend = new { dates = revenueTrendDates, amounts = revenueTrendAmounts },
                    paymentStatus = paymentStatusBreakdown,
                    topEvents = new
                    {
                        names = topEvents.Select(e => e.name).ToList(),
                        revenue = topEvents.Select(e => e.revenue).ToList()
                    },
                    ticketDistribution = new
                    {
                        categories = ticketDistribution.Select(t => t.category).ToList(),
                        counts = ticketDistribution.Select(t => t.count).ToList()
                    }
                };

                return Json(new { success = true, metrics, charts, sales });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales report data");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: Event Analytics Page
        /// </summary>
        public async Task<IActionResult> EventAnalytics()
        {
            if (!IsOrganizer())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            var organizerId = GetCurrentOrganizerId();
            var events = await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            ViewBag.Events = events;
            return View();
        }

        /// <summary>
        /// POST: Get Event Analytics Data (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetEventAnalyticsData([FromBody] EventAnalyticsRequest request)
        {
            try
            {
                if (!IsOrganizer() || string.IsNullOrEmpty(request.EventId) || !int.TryParse(request.EventId, out int eventId))
                    return BadRequest();

                var organizerId = GetCurrentOrganizerId();

                // Get event - ✅ FIXED: Added Customer include
                var @event = await _context.Events
                    .Include(e => e.Venue)
                    .Include(e => e.Bookings)
                        .ThenInclude(b => b.Tickets)
                    .Include(e => e.Bookings)
                        .ThenInclude(b => b.Payment)
                    .Include(e => e.Bookings)  // ✅ ADDED
                        .ThenInclude(b => b.Customer)  // ✅ ADDED
                    .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);

                if (@event == null)
                    return NotFound();

                // Calculate date range
                var startDate = request.TimeFrame == "all"
                    ? DateTime.MinValue
                    : DateTime.UtcNow.AddDays(-int.Parse(request.TimeFrame));

                // Filter bookings by date
                var bookings = @event.Bookings
                    .Where(b => b.BookingDate >= startDate)
                    .ToList();

                // Get tickets
                var tickets = bookings.SelectMany(b => b.Tickets).ToList();

                // Event info
                var eventInfo = new
                {
                    imageUrl = @event.ImageUrl,
                    title = @event.Title,
                    eventDate = @event.EventDate,
                    venueName = @event.Venue?.Name,
                    capacity = @event.Venue?.Capacity
                };

                // Metrics
                var ticketsSold = bookings.Sum(b => b.Quantity);
                var ticketsUsed = tickets.Count(t => t.Status == TicketStatus.Used);
                var occupancyRate = @event.Venue != null && @event.Venue.Capacity > 0
                    ? (int)((ticketsSold / (decimal)@event.Venue.Capacity) * 100)
                    : 0;

                var metrics = new
                {
                    ticketsSold,
                    ticketsUsed,
                    occupancyRate,
                    redemptionRate = ticketsSold > 0 ? (int)((ticketsUsed / (decimal)ticketsSold) * 100) : 0,
                    totalRevenue = bookings
                        .Where(b => b.Payment != null && b.Payment.Status == PaymentStatus.Completed)
                        .Sum(b => b.TotalAmount)
                };

                // Booking timeline
                var bookingTimelineDates = new List<string>();
                var bookingTimelineCounts = new List<int>();

                var currentDate = startDate;
                while (currentDate <= DateTime.UtcNow)
                {
                    var dayCount = bookings.Count(b => b.BookingDate.Date == currentDate.Date);
                    bookingTimelineDates.Add(currentDate.ToString("MMM dd"));
                    bookingTimelineCounts.Add(dayCount);
                    currentDate = currentDate.AddDays(1);
                }

                // Ticket status distribution
                var ticketStatusData = new[]
                {
            tickets.Count(t => t.Status == TicketStatus.Active),
            tickets.Count(t => t.Status == TicketStatus.Used),
            tickets.Count(t => t.Status == TicketStatus.Cancelled),
            tickets.Count(t => t.Status == TicketStatus.Expired)
        };

                // Daily attendance (check-ins)
                var dailyAttendanceDates = new List<string>();
                var dailyAttendanceCounts = new List<int>();

                currentDate = @event.EventDate.Date;
                var endDate = @event.EventDate.AddDays(1).Date;

                while (currentDate < endDate)
                {
                    var dayCheckins = tickets.Count(t => t.UsedDate?.Date == currentDate);
                    dailyAttendanceDates.Add(currentDate.ToString("MMM dd HH:mm"));
                    dailyAttendanceCounts.Add(dayCheckins);
                    currentDate = currentDate.AddHours(2);
                }

                // Payment methods
                var paymentMethods = bookings
                    .Where(b => b.Payment != null)
                    .GroupBy(b => b.Payment.PaymentMethod)
                    .Select(g => new { method = g.Key, count = g.Count() })
                    .ToList();

                var charts = new
                {
                    bookingTimeline = new
                    {
                        dates = bookingTimelineDates,
                        counts = bookingTimelineCounts
                    },
                    ticketStatus = ticketStatusData,
                    dailyAttendance = new
                    {
                        dates = dailyAttendanceDates,
                        checkins = dailyAttendanceCounts
                    },
                    paymentMethods = new
                    {
                        methods = paymentMethods.Select(p => p.method).ToList(),
                        counts = paymentMethods.Select(p => p.count).ToList()
                    }
                };

                // Booking details - ✅ NOW WORKS: Customer is not null
                var bookingDetails = bookings
                    .Select(b => new
                    {
                        bookingReference = b.BookingReference ?? $"BK{b.Id}",
                        customerName = b.Customer.Name,  // ✅ SAFE: Customer is now loaded
                        bookingDate = b.BookingDate,
                        quantity = b.Quantity,
                        ticketStatus = b.Tickets.Count > 0
                            ? (b.Tickets.Any(t => t.Status == TicketStatus.Used) ? "Used" : "Active")
                            : "Pending",
                        totalAmount = b.TotalAmount,
                        discountAmount = 0m
                    })
                    .OrderByDescending(b => b.bookingDate)
                    .ToList();

                // Insights
                var insights = GenerateAnalyticsInsights(metrics, @event, ticketsSold);

                return Json(new { success = true, eventInfo, metrics, charts, bookings = bookingDetails, insights });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event analytics data");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Generate insights based on analytics
        /// </summary>
        private List<object> GenerateAnalyticsInsights(dynamic metrics, Event @event, int ticketsSold)
        {
            var insights = new List<object>();

            // Occupancy insight
            if (metrics.occupancyRate >= 90)
            {
                insights.Add(new
                {
                    type = "positive",
                    title = "Excellent Occupancy",
                    description = $"Your event has reached {metrics.occupancyRate}% capacity. Outstanding performance!"
                });
            }
            else if (metrics.occupancyRate < 30)
            {
                insights.Add(new
                {
                    type = "warning",
                    title = "Low Occupancy",
                    description = $"Only {metrics.occupancyRate}% capacity filled. Consider marketing strategies to increase attendance."
                });
            }

            // Redemption insight
            if (metrics.redemptionRate >= 80)
            {
                insights.Add(new
                {
                    type = "positive",
                    title = "High Ticket Redemption",
                    description = $"{metrics.redemptionRate}% of tickets were redeemed. Great attendance rate!"
                });
            }

            // Revenue insight
            if (metrics.totalRevenue > 0)
            {
                insights.Add(new
                {
                    type = "positive",
                    title = "Revenue Generated",
                    description = $"Total revenue of Rs. {metrics.totalRevenue:N0} from {ticketsSold} tickets sold."
                });
            }

            // Event date insight
            if (@event.EventDate < DateTime.UtcNow)
            {
                insights.Add(new
                {
                    type = "info",
                    title = "Event Completed",
                    description = "This event has concluded. Review the analytics to plan future events."
                });
            }

            return insights;
        }

        /// <summary>
        /// Export Sales Report to CSV
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportSalesReport(string timeFrame = "30")
        {
            if (!IsOrganizer())
                return Unauthorized();

            var organizerId = GetCurrentOrganizerId();
            var startDate = timeFrame == "all"
                ? DateTime.MinValue
                : DateTime.UtcNow.AddDays(-int.Parse(timeFrame));

            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .Include(b => b.Payment)
                .Where(b => b.Event.OrganizerId == organizerId && b.BookingDate >= startDate)
                .ToListAsync();

            // Generate CSV
            var csv = "Booking Ref,Event,Customer,Date,Quantity,Revenue,Status\n";
            foreach (var booking in bookings)
            {
                csv += $"{booking.BookingReference},{booking.Event.Title},{booking.Customer.Name}," +
                       $"{booking.BookingDate:yyyy-MM-dd},{booking.Quantity},{booking.TotalAmount}," +
                       $"{booking.Payment?.Status.ToString() ?? "Pending"}\n";
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"sales-report-{DateTime.Now:yyyy-MM-dd}.csv");
        }

        /// <summary>
        /// Export Event Analytics to CSV
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportEventAnalytics(int eventId)
        {
            if (!IsOrganizer())
                return Unauthorized();

            var organizerId = GetCurrentOrganizerId();
            var @event = await _context.Events
                .Include(e => e.Bookings)
                    .ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);

            if (@event == null)
                return NotFound();

            // Generate CSV
            var csv = $"Event Analytics Report - {@event.Title}\n";
            csv += $"Event Date,Venue,Total Capacity\n";
            csv += $"{@event.EventDate:yyyy-MM-dd},{@event.Venue?.Name},{@event.Venue?.Capacity}\n\n";
            csv += "Booking Ref,Customer,Booking Date,Quantity,Amount\n";

            foreach (var booking in @event.Bookings)
            {
                csv += $"{booking.BookingReference},{booking.Customer.Name}," +
                       $"{booking.BookingDate:yyyy-MM-dd},{booking.Quantity},{booking.TotalAmount}\n";
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"analytics-{@event.Id}-{DateTime.Now:yyyy-MM-dd}.csv");
        }

        #endregion
    }
    // Request models for AJAX calls
    public class SalesReportRequest
    {
        public string TimeFrame { get; set; } = "30";
        public string? EventId { get; set; }
        public string? PaymentStatus { get; set; }
    }

    public class EventAnalyticsRequest
    {
        public string EventId { get; set; }
        public string TimeFrame { get; set; } = "30";
        public string? CompareWith { get; set; }
    }
    public class VerifyTicketRequest
    {
        public string QRCodeData { get; set; } = string.Empty;
    }
}