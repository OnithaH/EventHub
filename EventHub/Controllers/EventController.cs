using EventHub.Models.Entities;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    public class EventController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IUserService _userService;

        public EventController(IEventService eventService, IUserService userService)
        {
            _eventService = eventService;
            _userService = userService;
        }

        // GET: /Event
        public async Task<IActionResult> Index(string? category, DateTime? date, string? location, string? search)
        {
            IEnumerable<Event> events;

            if (!string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(category) || date.HasValue || !string.IsNullOrEmpty(location))
            {
                events = await _eventService.SearchEventsAsync(category, date, location);

                // Additional text search if provided
                if (!string.IsNullOrEmpty(search))
                {
                    events = events.Where(e =>
                        e.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        e.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        e.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
                }
            }
            else
            {
                events = await _eventService.GetAllEventsAsync();
            }

            // Pass search parameters to view for maintaining form state
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentDate = date?.ToString("yyyy-MM-dd");
            ViewBag.CurrentLocation = location;
            ViewBag.CurrentSearch = search;

            return View(events);
        }

        // GET: /Event/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var eventModel = await _eventService.GetEventByIdAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            return View(eventModel);
        }

        // GET: /Event/Create (For Organizers only)
        public IActionResult Create()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Organizer" && userRole != "Admin")
            {
                TempData["ErrorMessage"] = "You must be an event organizer to create events.";
                return RedirectToAction("Index");
            }

            return View();
        }

        // POST: /Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event eventModel)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Organizer" && userRole != "Admin")
            {
                return Forbid();
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                eventModel.OrganizerId = int.Parse(userIdString);
                eventModel.IsActive = true;

                try
                {
                    await _eventService.CreateEventAsync(eventModel);
                    TempData["SuccessMessage"] = "Event created successfully!";
                    return RedirectToAction("Details", new { id = eventModel.Id });
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while creating the event. Please try again.");
                    // Log exception in production
                }
            }

            return View(eventModel);
        }

        // GET: /Event/Edit/5 (For Organizers only)
        public async Task<IActionResult> Edit(int id)
        {
            var eventModel = await _eventService.GetEventByIdAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            // Check if current user is the organizer or admin
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) ||
                (userRole != "Admin" && eventModel.OrganizerId != int.Parse(userIdString)))
            {
                TempData["ErrorMessage"] = "You can only edit your own events.";
                return RedirectToAction("Details", new { id });
            }

            return View(eventModel);
        }

        // POST: /Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event eventModel)
        {
            if (id != eventModel.Id)
            {
                return BadRequest();
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) ||
                (userRole != "Admin" && eventModel.OrganizerId != int.Parse(userIdString)))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _eventService.UpdateEventAsync(eventModel);
                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction("Details", new { id });
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while updating the event. Please try again.");
                    // Log exception in production
                }
            }

            return View(eventModel);
        }

        // POST: /Event/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var eventModel = await _eventService.GetEventByIdAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) ||
                (userRole != "Admin" && eventModel.OrganizerId != int.Parse(userIdString)))
            {
                return Forbid();
            }

            try
            {
                await _eventService.DeleteEventAsync(id);
                TempData["SuccessMessage"] = "Event deleted successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the event.";
                // Log exception in production
            }

            return RedirectToAction("Index");
        }

        // GET: /Event/Search (AJAX endpoint)
        [HttpGet]
        public async Task<IActionResult> Search(string? term, string? category)
        {
            var events = await _eventService.SearchEventsAsync(category);

            if (!string.IsNullOrEmpty(term))
            {
                events = events.Where(e =>
                    e.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    e.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var result = events.Select(e => new {
                id = e.Id,
                title = e.Title,
                description = e.Description,
                ticketPrice = e.TicketPrice,
                eventDate = e.EventDate.ToString("yyyy-MM-dd"),
                venue = e.Venue?.Name,
                availableTickets = e.AvailableTickets
            });

            return Json(result);
        }
        // Add this method to handle search form from homepage
        [HttpGet]
        public async Task<IActionResult> Search(string searchTerm, string category, string location)
        {
            IEnumerable<Event> events;

            if (!string.IsNullOrEmpty(searchTerm) || !string.IsNullOrEmpty(category) || !string.IsNullOrEmpty(location))
            {
                events = await _eventService.SearchEventsAsync(category, null, location);

                // Additional text search if provided
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    events = events.Where(e =>
                        e.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        e.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }
            }
            else
            {
                events = await _eventService.GetAllEventsAsync();
            }

            // Pass search parameters to view for maintaining form state
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentLocation = location;
            ViewBag.CurrentSearch = searchTerm;

            return View("Index", events);
        }
    }
}