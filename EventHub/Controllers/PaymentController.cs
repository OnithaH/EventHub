using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Models.ViewModels;
using EventHub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IQRCodeService _qrCodeService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context,
            IQRCodeService qrCodeService,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _logger = logger;
        }

        /// <summary>
        /// Display payment history
        /// GET: /Payment/History
        /// </summary>
        public async Task<IActionResult> History(string searchTerm = "", string dateFilter = "", int page = 1)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                var userRole = HttpContext.Session.GetString("UserRole");

                if (string.IsNullOrEmpty(userIdString) || userRole != "Customer")
                {
                    TempData["ErrorMessage"] = "Please log in to view payment history.";
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                // Query payments
                var query = _context.Payments
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Event)
                    .Where(p => p.Booking.CustomerId == customerId);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(p =>
                        p.Booking.Event.Title.Contains(searchTerm) ||
                        p.Booking.BookingReference.Contains(searchTerm));
                }

                var now = DateTime.UtcNow;
                query = dateFilter switch
                {
                    "today" => query.Where(p => p.PaymentDate.Date == now.Date),
                    "week" => query.Where(p => p.PaymentDate >= now.AddDays(-7)),
                    "month" => query.Where(p => p.PaymentDate >= now.AddMonths(-1)),
                    "year" => query.Where(p => p.PaymentDate >= now.AddYears(-1)),
                    _ => query
                };

                // Statistics
                var allPayments = await query.ToListAsync();
                var totalPayments = allPayments.Count;
                var successfulPayments = allPayments.Count(p => p.Status == PaymentStatus.Completed);
                var failedPayments = allPayments.Count(p => p.Status == PaymentStatus.Failed);
                var totalAmountPaid = allPayments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);

                // Pagination
                var pageSize = 10;
                var totalPages = (int)Math.Ceiling(allPayments.Count / (double)pageSize);

                var payments = allPayments
                    .OrderByDescending(p => p.PaymentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PaymentDisplayDto
                    {
                        Id = p.Id,
                        BookingId = p.Booking.Id,
                        BookingReference = p.Booking.BookingReference ?? $"BK{p.Booking.Id:D6}",
                        Amount = p.Amount,
                        PaymentDate = p.PaymentDate,
                        PaymentMethod = p.PaymentMethod,
                        Status = p.Status.ToString(),
                        EventTitle = p.Booking.Event.Title,
                        EventDate = p.Booking.Event.EventDate
                    })
                    .ToList();

                var viewModel = new PaymentHistoryViewModel
                {
                    Payments = payments,
                    TotalPayments = totalPayments,
                    SuccessfulPayments = successfulPayments,
                    FailedPayments = failedPayments,
                    TotalAmountPaid = totalAmountPaid,
                    SearchTerm = searchTerm,
                    DateFilter = dateFilter,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment history");
                TempData["ErrorMessage"] = "Unable to load payment history.";
                return RedirectToAction("Dashboard", "Customer");
            }
        }

        /// <summary>
        /// Display payment success page
        /// GET: /Payment/Success/5
        /// </summary>
        public async Task<IActionResult> Success(int id)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                var payment = await _context.Payments
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Event)
                            .ThenInclude(e => e.Venue)
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Tickets)
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Customer)
                    .FirstOrDefaultAsync(p => p.Id == id &&
                                            p.Booking.CustomerId == customerId);

                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                var viewModel = new PaymentSuccessViewModel
                {
                    BookingId = payment.Booking.Id,
                    BookingReference = payment.Booking.BookingReference ?? $"BK{payment.Booking.Id:D6}",
                    PaymentId = payment.Id,
                    AmountPaid = payment.Amount,
                    EventTitle = payment.Booking.Event.Title,
                    EventDate = payment.Booking.Event.EventDate,
                    VenueName = payment.Booking.Event.Venue.Name,
                    TicketCount = payment.Booking.Tickets.Count,
                    LoyaltyPointsEarned = (int)payment.Amount, // 1 point per dollar
                    CustomerEmail = payment.Booking.Customer.Email
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment success page");
                TempData["ErrorMessage"] = "Unable to load payment confirmation.";
                return RedirectToAction("MyBookings", "Booking");
            }
        }

        /// <summary>
        /// Process payment
        /// POST: /Payment/ProcessPayment
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel model)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                // Get booking
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Customer)
                    .FirstOrDefaultAsync(b => b.Id == model.BookingId &&
                                            b.CustomerId == customerId);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // Validate booking status
                if (booking.Status != BookingStatus.Pending)
                {
                    TempData["ErrorMessage"] = "This booking has already been processed.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // Create payment record
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    Amount = model.Amount,
                    PaymentMethod = model.PaymentMethod,
                    PaymentDate = DateTime.UtcNow,
                    Status = PaymentStatus.Completed,
                    TransactionId = $"TXN{DateTime.UtcNow.Ticks}",
                    PaymentDetails = $"Payment via {model.PaymentMethod}"
                };

                _context.Payments.Add(payment);

                // Update booking status
                booking.Status = BookingStatus.Confirmed;
                booking.BookingReference = $"BK{booking.Id:D6}";

                // Generate tickets with QR codes
                for (int i = 0; i < booking.Quantity; i++)
                {
                    var ticketNumber = $"TKT{booking.Id:D6}-{i + 1:D3}";

                    // Generate QR code and store as string
                    var qrCodeString = _qrCodeService.GenerateQRCode(ticketNumber);

                    var ticket = new Ticket
                    {
                        BookingId = booking.Id,
                        TicketNumber = ticketNumber,
                        QRCode = qrCodeString,  // ✅ Store QR code Base64 string here
                        Status = TicketStatus.Active,
                        IssuedDate = DateTime.UtcNow
                    };

                    _context.Tickets.Add(ticket);
                }

                // Update customer loyalty points (1 point per dollar)
                booking.Customer.LoyaltyPoints += (int)model.Amount;

                // Update available tickets
                booking.Event.AvailableTickets -= booking.Quantity;

                // Save all changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment processed successfully for booking {BookingId}", booking.Id);

                // Redirect to success page
                return RedirectToAction("Success", new { id = payment.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                TempData["ErrorMessage"] = "An error occurred while processing your payment. Please try again.";
                return RedirectToAction("Checkout", "Booking", new { id = model.BookingId });
            }
        }

        /// <summary>
        /// Download payment receipt
        /// GET: /Payment/Receipt/5
        /// </summary>
        public async Task<IActionResult> Receipt(int id)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);

                var payment = await _context.Payments
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Event)
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Customer)
                    .FirstOrDefaultAsync(p => p.Id == id &&
                                            p.Booking.CustomerId == customerId);

                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction("History");
                }

                // TODO: Generate PDF receipt
                var receiptContent = $"EVENTHUB PAYMENT RECEIPT\n" +
                                   $"======================\n" +
                                   $"Receipt #: {payment.TransactionId}\n" +
                                   $"Date: {payment.PaymentDate:MMM dd, yyyy HH:mm}\n" +
                                   $"Amount: {payment.Amount:C}\n" +
                                   $"Method: {payment.PaymentMethod}\n" +
                                   $"Status: {payment.Status}\n" +
                                   $"Event: {payment.Booking.Event.Title}\n" +
                                   $"Booking Ref: {payment.Booking.BookingReference}\n";

                var bytes = System.Text.Encoding.UTF8.GetBytes(receiptContent);
                return File(bytes, "text/plain", $"Receipt-{payment.TransactionId}.txt");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt");
                TempData["ErrorMessage"] = "Unable to generate receipt.";
                return RedirectToAction("History");
            }
        }
    }
}