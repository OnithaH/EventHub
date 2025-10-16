using EventHub.Data;
using EventHub.Models.Entities;
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
        private readonly ILogger<OrganizerController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OrganizerController(
            ApplicationDbContext context,
            IEventService eventService,
            IUserService userService,
            ILogger<OrganizerController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _eventService = eventService;
            _userService = userService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
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

                // ✅ FIX: Remove VenueId validation if creating new venue
                if (!string.IsNullOrEmpty(NewVenueName))
                {
                    ModelState.Remove("VenueId");
                }

                // 🔧 FIX: Convert EventDate to UTC
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

                // Handle image upload
                if (eventImage != null && eventImage.Length > 0)
                {
                    var fileName = $"event-{Guid.NewGuid()}{Path.GetExtension(eventImage.FileName)}";
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "events");
                    Directory.CreateDirectory(uploadsFolder);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await eventImage.CopyToAsync(stream);
                    }

                    eventModel.ImageUrl = $"/images/events/{fileName}";
                }
                else
                {
                    eventModel.ImageUrl = "/images/events/default-event.jpg";
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
            var existingEvent = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            if (existingEvent.OrganizerId != organizerId && HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload if new image provided
                    if (eventImage != null && eventImage.Length > 0)
                    {
                        // Delete old image if it exists and is not default
                        if (!string.IsNullOrEmpty(existingEvent.ImageUrl) &&
                            existingEvent.ImageUrl != "/images/events/default-event.jpg")
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                                existingEvent.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        var fileName = $"event-{Guid.NewGuid()}{Path.GetExtension(eventImage.FileName)}";
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "events", fileName);

                        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await eventImage.CopyToAsync(stream);
                        }

                        eventModel.ImageUrl = $"/images/events/{fileName}";
                    }
                    else
                    {
                        // Keep existing image
                        eventModel.ImageUrl = existingEvent.ImageUrl;
                    }

                    eventModel.OrganizerId = existingEvent.OrganizerId;
                    eventModel.CreatedAt = existingEvent.CreatedAt;

                    await _eventService.UpdateEventAsync(eventModel);

                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction(nameof(MyEvents));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating event");
                    ModelState.AddModelError("", "An error occurred while updating the event.");
                }
            }

            ViewBag.Venues = await _context.Venues
                .OrderBy(v => v.Name)
                .Select(v => new { v.Id, v.Name, v.Location, v.Capacity })
                .ToListAsync();

            ViewBag.BookingCount = await _context.Bookings.CountAsync(b => b.EventId == id);

            return View(eventModel);
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
                    return NotFound();
                }

                var organizerId = GetCurrentOrganizerId();
                if (eventModel.OrganizerId != organizerId && HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    return Forbid();
                }

                // Check if event has bookings
                var hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);
                if (hasBookings)
                {
                    TempData["ErrorMessage"] = "Cannot delete event with existing bookings. Please cancel bookings first.";
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
                    }
                }

                await _eventService.DeleteEventAsync(id);

                TempData["SuccessMessage"] = "Event deleted successfully!";
                return RedirectToAction(nameof(MyEvents));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event");
                TempData["ErrorMessage"] = "An error occurred while deleting the event.";
                return RedirectToAction(nameof(MyEvents));
            }
        }
        #endregion
    }
}