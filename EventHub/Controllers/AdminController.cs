using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Models.ViewModels;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEventService _eventService;
        private readonly IVenueService _venueService;
        private readonly IUserService _userService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IEventService eventService,
            IVenueService venueService,
            IUserService userService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _eventService = eventService;
            _venueService = venueService;
            _userService = userService;
            _logger = logger;
        }

        // Authorization Check
        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Admin";
        }

        public IActionResult Index()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // ========== EVENT MANAGEMENT ==========

        // GET: Admin/ManageEvents
        public async Task<IActionResult> ManageEvents(EventFilterViewModel filters)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Venue)
                    .Include(e => e.Bookings)
                    .ToListAsync();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    events = events.Where(e =>
                        e.Title.Contains(filters.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        e.Description.Contains(filters.SearchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(filters.Category))
                {
                    events = events.Where(e => e.Category.Equals(filters.Category, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(filters.OrganizerName))
                {
                    events = events.Where(e => e.Organizer != null &&
                        e.Organizer.Name.Contains(filters.OrganizerName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (filters.StartDate.HasValue)
                {
                    events = events.Where(e => e.EventDate >= filters.StartDate.Value).ToList();
                }

                if (filters.EndDate.HasValue)
                {
                    events = events.Where(e => e.EventDate <= filters.EndDate.Value).ToList();
                }

                if (filters.IsActive.HasValue)
                {
                    events = events.Where(e => e.IsActive == filters.IsActive.Value).ToList();
                }

                // Sort events
                events = filters.SortBy switch
                {
                    "title" => events.OrderBy(e => e.Title).ToList(),
                    "date_asc" => events.OrderBy(e => e.EventDate).ToList(),
                    "date_desc" => events.OrderByDescending(e => e.EventDate).ToList(),
                    "price" => events.OrderBy(e => e.TicketPrice).ToList(),
                    _ => events.OrderByDescending(e => e.CreatedAt).ToList()
                };

                // Create view models
                var eventViewModels = events.Select(e => new AdminEventViewModel
                {
                    Event = e,
                    OrganizerName = e.Organizer?.Name ?? "Unknown",
                    VenueName = e.Venue?.Name ?? "Unknown",
                    TotalBookings = e.Bookings?.Count ?? 0,
                    TicketsSold = e.TotalTickets - e.AvailableTickets,
                    TotalRevenue = e.Bookings?.Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
                                             .Sum(b => b.TotalAmount) ?? 0
                }).ToList();

                var viewModel = new AdminEventsListViewModel
                {
                    Events = eventViewModels,
                    Filters = filters,
                    TotalEvents = eventViewModels.Count,
                    ActiveEvents = eventViewModels.Count(e => e.Event.IsActive),
                    InactiveEvents = eventViewModels.Count(e => !e.Event.IsActive),
                    UpcomingEvents = eventViewModels.Count(e => e.Event.EventDate >= DateTime.UtcNow),
                    PastEvents = eventViewModels.Count(e => e.Event.EventDate < DateTime.UtcNow)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin events management");
                TempData["ErrorMessage"] = "An error occurred while loading events.";
                return RedirectToAction("Index");
            }
        }

        // GET: Admin/EditEvent/5
        public async Task<IActionResult> EditEvent(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var eventModel = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventModel == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("ManageEvents");
            }

            ViewBag.Venues = await _context.Venues.OrderBy(v => v.Name).ToListAsync();
            ViewBag.Organizers = await _context.Users
                .Where(u => u.Role == UserRole.Organizer)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return View(eventModel);
        }

        // POST: Admin/EditEvent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, Event eventModel)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (id != eventModel.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eventModel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction("ManageEvents");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating event {EventId}", id);
                    ModelState.AddModelError("", "An error occurred while updating the event.");
                }
            }

            ViewBag.Venues = await _context.Venues.OrderBy(v => v.Name).ToListAsync();
            ViewBag.Organizers = await _context.Users
                .Where(u => u.Role == UserRole.Organizer)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return View(eventModel);
        }

        // POST: Admin/ToggleEventStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEventStatus(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var eventModel = await _context.Events.FindAsync(id);
                if (eventModel == null)
                {
                    return Json(new { success = false, message = "Event not found" });
                }

                eventModel.IsActive = !eventModel.IsActive;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Event {(eventModel.IsActive ? "activated" : "deactivated")} successfully",
                    isActive = eventModel.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling event status");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: Admin/DeleteEvent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var eventModel = await _context.Events
                    .Include(e => e.Bookings)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (eventModel == null)
                {
                    return Json(new { success = false, message = "Event not found" });
                }

                // Check if event has bookings
                if (eventModel.Bookings.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete event with existing bookings. Please deactivate instead."
                    });
                }

                _context.Events.Remove(eventModel);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Event deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event");
                return Json(new { success = false, message = "An error occurred while deleting the event" });
            }
        }

        // ========== VENUE MANAGEMENT ==========

        // GET: Admin/ManageVenues
        public async Task<IActionResult> ManageVenues(string searchTerm)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var venues = await _venueService.SearchVenuesAsync(searchTerm);

                var venueViewModels = new List<AdminVenueViewModel>();
                foreach (var venue in venues)
                {
                    var eventCount = await _venueService.GetEventCountByVenueAsync(venue.Id);
                    var hasActiveEvents = await _venueService.VenueHasActiveEventsAsync(venue.Id);

                    venueViewModels.Add(new AdminVenueViewModel
                    {
                        Venue = venue,
                        EventCount = eventCount,
                        ActiveEventCount = venue.Events.Count(e => e.IsActive && e.EventDate >= DateTime.UtcNow),
                        UpcomingEventCount = venue.Events.Count(e => e.EventDate >= DateTime.UtcNow),
                        HasActiveEvents = hasActiveEvents
                    });
                }

                var viewModel = new AdminVenuesListViewModel
                {
                    Venues = venueViewModels,
                    SearchTerm = searchTerm,
                    TotalVenues = venueViewModels.Count,
                    VenuesWithEvents = venueViewModels.Count(v => v.EventCount > 0)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading venue management");
                TempData["ErrorMessage"] = "An error occurred while loading venues.";
                return RedirectToAction("Index");
            }
        }

        // GET: Admin/CreateVenue
        public IActionResult CreateVenue()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            return View(new Venue());
        }

        // POST: Admin/CreateVenue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVenue(Venue venue)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _venueService.CreateVenueAsync(venue);
                    TempData["SuccessMessage"] = "Venue created successfully!";
                    return RedirectToAction("ManageVenues");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating venue");
                    ModelState.AddModelError("", "An error occurred while creating the venue.");
                }
            }

            return View(venue);
        }

        // GET: Admin/EditVenue/5
        public async Task<IActionResult> EditVenue(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var venue = await _venueService.GetVenueByIdAsync(id);
            if (venue == null)
            {
                TempData["ErrorMessage"] = "Venue not found.";
                return RedirectToAction("ManageVenues");
            }

            return View(venue);
        }

        // POST: Admin/EditVenue/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(int id, Venue venue)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (id != venue.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _venueService.UpdateVenueAsync(venue);
                    TempData["SuccessMessage"] = "Venue updated successfully!";
                    return RedirectToAction("ManageVenues");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating venue");
                    ModelState.AddModelError("", "An error occurred while updating the venue.");
                }
            }

            return View(venue);
        }

        // POST: Admin/DeleteVenue/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var hasActiveEvents = await _venueService.VenueHasActiveEventsAsync(id);
                if (hasActiveEvents)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete venue with active events. Please cancel or complete events first."
                    });
                }

                var result = await _venueService.DeleteVenueAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Venue deleted successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Venue not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting venue");
                return Json(new { success = false, message = "An error occurred while deleting the venue" });
            }
        }
    }
}