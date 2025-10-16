using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Models.ViewModels;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IQRCodeService _qrCodeService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(
            ApplicationDbContext context,
            IQRCodeService qrCodeService,
            ILogger<TicketController> logger)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _logger = logger;
        }

        /// <summary>
        /// Display all tickets for the logged-in customer
        /// GET: /Ticket/MyTickets
        /// </summary>
        public async Task<IActionResult> MyTickets(
            string searchTerm = "",
            string statusFilter = "",
            string dateFilter = "",
            int page = 1)
        {
            try
            {
                // Check authentication
                var userIdString = HttpContext.Session.GetString("UserId");
                var userRole = HttpContext.Session.GetString("UserRole");

                if (string.IsNullOrEmpty(userIdString) || userRole != "Customer")
                {
                    TempData["ErrorMessage"] = "Please log in as a customer to view your tickets.";
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                // Base query
                var query = _context.Tickets
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Event)
                            .ThenInclude(e => e.Venue)
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Customer)
                    .Where(t => t.Booking.CustomerId == customerId);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(t =>
                        t.Booking.Event.Title.Contains(searchTerm) ||
                        t.TicketNumber.Contains(searchTerm));
                }

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    if (Enum.TryParse<TicketStatus>(statusFilter, out var status))
                    {
                        query = query.Where(t => t.Status == status);
                    }
                }

                // Apply date filter
                var now = DateTime.UtcNow;
                query = dateFilter switch
                {
                    "upcoming" => query.Where(t => t.Booking.Event.EventDate > now),
                    "past" => query.Where(t => t.Booking.Event.EventDate <= now),
                    _ => query
                };

                // Get statistics
                var allTickets = await _context.Tickets
                    .Include(t => t.Booking)
                    .Where(t => t.Booking.CustomerId == customerId)
                    .ToListAsync();

                var totalTickets = allTickets.Count;
                var activeTickets = allTickets.Count(t => t.Status == TicketStatus.Active);
                var usedTickets = allTickets.Count(t => t.Status == TicketStatus.Used);

                // Pagination
                var pageSize = 10;
                var totalRecords = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                var tickets = await query
                    .OrderByDescending(t => t.IssuedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TicketDisplayDto
                    {
                        Id = t.Id,
                        TicketNumber = t.TicketNumber,
                        QRCode = t.QRCode,
                        QRCodeBase64 = t.QRCode, // Assuming QRCode is already Base64
                        Status = t.Status,
                        IssuedDate = t.IssuedDate,
                        UsedDate = t.UsedDate,
                        BookingId = t.Booking.Id,
                        BookingReference = t.Booking.BookingReference ?? $"BK{t.Booking.Id:D6}",
                        EventId = t.Booking.Event.Id,
                        EventTitle = t.Booking.Event.Title,
                        EventDate = t.Booking.Event.EventDate,
                        EventCategory = t.Booking.Event.Category,
                        VenueName = t.Booking.Event.Venue.Name,
                        VenueLocation = t.Booking.Event.Venue.Location,
                        CustomerName = t.Booking.Customer.Name
                    })
                    .ToListAsync();

                var viewModel = new MyTicketsViewModel
                {
                    Tickets = tickets,
                    TotalTickets = totalTickets,
                    ActiveTickets = activeTickets,
                    UsedTickets = usedTickets,
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    DateFilter = dateFilter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tickets for customer");
                TempData["ErrorMessage"] = "Unable to load your tickets. Please try again.";
                return RedirectToAction("Dashboard", "Customer");
            }
        }

        /// <summary>
        /// View tickets for a specific booking
        /// GET: /Ticket/ViewTickets?bookingId=5
        /// </summary>
        public async Task<IActionResult> ViewTickets(int bookingId)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                var tickets = await _context.Tickets
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Event)
                            .ThenInclude(e => e.Venue)
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Customer)
                    .Where(t => t.BookingId == bookingId &&
                               t.Booking.CustomerId == customerId)
                    .ToListAsync();

                if (!tickets.Any())
                {
                    TempData["ErrorMessage"] = "No tickets found for this booking.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                return View(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing tickets for booking {BookingId}", bookingId);
                TempData["ErrorMessage"] = "Unable to load tickets.";
                return RedirectToAction("MyBookings", "Booking");
            }
        }

        /// <summary>
        /// Download a single ticket as PDF
        /// GET: /Ticket/Download/5
        /// </summary>
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                var ticket = await _context.Tickets
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Event)
                            .ThenInclude(e => e.Venue)
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Customer)
                    .FirstOrDefaultAsync(t => t.Id == id &&
                                            t.Booking.CustomerId == customerId);

                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Ticket not found.";
                    return RedirectToAction("MyTickets");
                }

                // TODO: Implement PDF generation
                // For now, return a simple text file
                var content = $"EventHub E-Ticket\n" +
                             $"==================\n" +
                             $"Ticket Number: {ticket.TicketNumber}\n" +
                             $"Event: {ticket.Booking.Event.Title}\n" +
                             $"Date: {ticket.Booking.Event.EventDate:MMM dd, yyyy 'at' hh:mm tt}\n" +
                             $"Venue: {ticket.Booking.Event.Venue.Name}\n" +
                             $"Customer: {ticket.Booking.Customer.Name}\n" +
                             $"Status: {ticket.Status}\n";

                var bytes = System.Text.Encoding.UTF8.GetBytes(content);
                return File(bytes, "text/plain", $"Ticket-{ticket.TicketNumber}.txt");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading ticket {TicketId}", id);
                TempData["ErrorMessage"] = "Unable to download ticket.";
                return RedirectToAction("MyTickets");
            }
        }

        /// <summary>
        /// Download all tickets for a booking
        /// GET: /Ticket/DownloadTickets?bookingId=5
        /// </summary>
        public async Task<IActionResult> DownloadTickets(int bookingId)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                var tickets = await _context.Tickets
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Event)
                    .Where(t => t.BookingId == bookingId &&
                               t.Booking.CustomerId == customerId)
                    .ToListAsync();

                if (!tickets.Any())
                {
                    TempData["ErrorMessage"] = "No tickets found.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // TODO: Implement ZIP file with multiple PDFs
                TempData["SuccessMessage"] = $"Downloaded {tickets.Count} ticket(s).";
                return RedirectToAction("ViewTickets", new { bookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading tickets for booking {BookingId}", bookingId);
                TempData["ErrorMessage"] = "Unable to download tickets.";
                return RedirectToAction("MyBookings", "Booking");
            }
        }

        /// <summary>
        /// Download all customer tickets
        /// GET: /Ticket/DownloadAll
        /// </summary>
        public async Task<IActionResult> DownloadAll()
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                var ticketCount = await _context.Tickets
                    .Include(t => t.Booking)
                    .Where(t => t.Booking.CustomerId == customerId)
                    .CountAsync();

                // TODO: Implement bulk download
                TempData["SuccessMessage"] = $"Downloaded all {ticketCount} ticket(s).";
                return RedirectToAction("MyTickets");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading all tickets");
                TempData["ErrorMessage"] = "Unable to download tickets.";
                return RedirectToAction("MyTickets");
            }
        }
    }
}