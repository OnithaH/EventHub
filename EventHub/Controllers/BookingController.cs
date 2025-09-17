using EventHub.Models.Entities;
using EventHub.Models.ViewModels;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;
        private readonly IUserService _userService;
        private readonly IQRCodeService _qrCodeService;

        public BookingController(
            IBookingService bookingService,
            IEventService eventService,
            IUserService userService,
            IQRCodeService qrCodeService)
        {
            _bookingService = bookingService;
            _eventService = eventService;
            _userService = userService;
            _qrCodeService = qrCodeService;
        }

        // GET: /Booking/Create/5
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

        // POST: /Booking/Create
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

                    // Generate tickets with QR codes
                    for (int i = 0; i < model.Quantity; i++)
                    {
                        var qrData = _qrCodeService.GenerateTicketQRData(
                            0, createdBooking.Id, model.EventId); // TicketId will be set after creation
                        var qrCode = _qrCodeService.GenerateQRCode(qrData);

                        var ticket = new Ticket
                        {
                            BookingId = createdBooking.Id,
                            QRCode = qrCode,
                            TicketNumber = $"TK{DateTime.UtcNow:yyyyMMdd}{createdBooking.Id:D6}{i + 1:D2}",
                            Status = TicketStatus.Active
                        };

                        // Add ticket to context (you'll need to add this to booking service)
                    }

                    // Update available tickets
                    await _eventService.UpdateAvailableTicketsAsync(model.EventId, model.Quantity);

                    TempData["SuccessMessage"] = "Booking created successfully! Please proceed with payment.";
                    return RedirectToAction("Details", new { id = createdBooking.Id });
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while creating your booking. Please try again.");
                }
            }

            return View(model);
        }

        // GET: /Booking/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            // Check if current user owns this booking or is admin
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) ||
                (userRole != "Admin" && booking.CustomerId != int.Parse(userIdString)))
            {
                return Forbid();
            }

            return View(booking);
        }

        // GET: /Booking/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _bookingService.GetBookingsByCustomerAsync(int.Parse(userIdString));
            return View(bookings);
        }

        // POST: /Booking/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || booking.CustomerId != int.Parse(userIdString))
            {
                return Forbid();
            }

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending bookings can be cancelled.";
                return RedirectToAction("Details", new { id });
            }

            try
            {
                await _bookingService.CancelBookingAsync(id);
                TempData["SuccessMessage"] = "Booking cancelled successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while cancelling the booking.";
            }

            return RedirectToAction("MyBookings");
        }
    }
}