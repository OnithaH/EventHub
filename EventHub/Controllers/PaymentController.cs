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
            ILogger<PaymentController> _logger)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            this._logger = _logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel model)
        {
            _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║          PAYMENT PROCESS STARTED                          ║");
            _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");

            _logger.LogInformation("📋 RECEIVED DATA:");
            _logger.LogInformation("   BookingId: {BookingId}", model.BookingId);
            _logger.LogInformation("   Amount: {Amount}", model.Amount);
            _logger.LogInformation("   PaymentMethod: {PaymentMethod}", model.PaymentMethod ?? "NULL/EMPTY");
            _logger.LogInformation("   FirstName: {FirstName}", model.FirstName ?? "NULL");
            _logger.LogInformation("   LastName: {LastName}", model.LastName ?? "NULL");
            _logger.LogInformation("   Email: {Email}", model.Email ?? "NULL");
            _logger.LogInformation("   Phone: {Phone}", model.Phone ?? "NULL");

            _logger.LogInformation("🔍 MODEL STATE CHECK:");
            _logger.LogInformation("   IsValid: {IsValid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                _logger.LogError("❌ MODEL STATE IS INVALID - VALIDATION ERRORS:");
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        _logger.LogError("   ❗ Field: {Field}", error.Key);
                        foreach (var err in error.Value.Errors)
                        {
                            _logger.LogError("      → {ErrorMessage}", err.ErrorMessage);
                        }
                    }
                }

                TempData["ErrorMessage"] = "Please fill all required fields correctly.";
                _logger.LogWarning("⚠️ REDIRECTING BACK TO CHECKOUT DUE TO VALIDATION ERRORS");
                return RedirectToAction("Checkout", "Booking", new { id = model.BookingId });
            }

            _logger.LogInformation("✅ MODEL STATE IS VALID - PROCEEDING");

            try
            {
                // Check session
                var userIdString = HttpContext.Session.GetString("UserId");
                _logger.LogInformation("🔐 SESSION CHECK:");
                _logger.LogInformation("   UserId from session: {UserId}", userIdString ?? "NULL/EMPTY");

                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogWarning("❌ USER NOT LOGGED IN - REDIRECTING TO LOGIN");
                    TempData["ErrorMessage"] = "Please log in to complete payment.";
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);
                _logger.LogInformation("✅ USER AUTHENTICATED - CustomerId: {CustomerId}", customerId);

                // Get booking
                _logger.LogInformation("📦 FETCHING BOOKING FROM DATABASE...");
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Customer)
                    .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.CustomerId == customerId);

                if (booking == null)
                {
                    _logger.LogError("❌ BOOKING NOT FOUND - BookingId: {BookingId}, CustomerId: {CustomerId}",
                        model.BookingId, customerId);
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                _logger.LogInformation("✅ BOOKING FOUND:");
                _logger.LogInformation("   Booking ID: {BookingId}", booking.Id);
                _logger.LogInformation("   Event: {EventTitle}", booking.Event.Title);
                _logger.LogInformation("   Current Status: {Status}", booking.Status);
                _logger.LogInformation("   Quantity: {Quantity}", booking.Quantity);
                _logger.LogInformation("   Amount: {Amount}", booking.TotalAmount);

                // Check status
                if (booking.Status != BookingStatus.Pending)
                {
                    _logger.LogWarning("⚠️ BOOKING ALREADY PROCESSED - Status: {Status}", booking.Status);
                    TempData["InfoMessage"] = "This booking has already been processed.";
                    return RedirectToAction("Details", "Booking", new { id = model.BookingId });
                }

                _logger.LogInformation("✅ BOOKING STATUS IS PENDING - CREATING PAYMENT RECORD...");

                // Create payment
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    Amount = model.Amount,
                    PaymentMethod = model.PaymentMethod ?? "CreditCard",
                    PaymentDate = DateTime.UtcNow,
                    Status = PaymentStatus.Completed,
                    TransactionId = $"TXN{DateTime.UtcNow.Ticks}",
                    PaymentDetails = $"Payment via {model.PaymentMethod}"
                };

                _context.Payments.Add(payment);
                _logger.LogInformation("💳 PAYMENT RECORD CREATED:");
                _logger.LogInformation("   Transaction ID: {TransactionId}", payment.TransactionId);
                _logger.LogInformation("   Method: {Method}", payment.PaymentMethod);
                _logger.LogInformation("   Amount: {Amount}", payment.Amount);

                // Update booking
                booking.Status = BookingStatus.Confirmed;
                booking.BookingReference = $"BK{booking.Id:D6}";
                _logger.LogInformation("📝 BOOKING UPDATED:");
                _logger.LogInformation("   New Status: {Status}", booking.Status);
                _logger.LogInformation("   Reference: {Reference}", booking.BookingReference);

                // Generate tickets
                _logger.LogInformation("🎫 GENERATING {Count} TICKETS...", booking.Quantity);
                for (int i = 0; i < booking.Quantity; i++)
                {
                    var ticketNumber = $"TKT{booking.Id:D6}-{i + 1:D3}";
                    var qrCodeString = _qrCodeService.GenerateQRCode(ticketNumber);

                    var ticket = new Ticket
                    {
                        BookingId = booking.Id,
                        TicketNumber = ticketNumber,
                        QRCode = qrCodeString,
                        Status = TicketStatus.Active,
                        IssuedDate = DateTime.UtcNow
                    };

                    _context.Tickets.Add(ticket);
                    _logger.LogInformation("   ✅ Ticket {Index}/{Total} created: {TicketNumber}",
                        i + 1, booking.Quantity, ticketNumber);
                }

                // Update loyalty points
                var pointsEarned = (int)model.Amount;
                booking.Customer.LoyaltyPoints += pointsEarned;
                _logger.LogInformation("⭐ LOYALTY POINTS UPDATED:");
                _logger.LogInformation("   Points Earned: {Points}", pointsEarned);
                _logger.LogInformation("   New Total: {Total}", booking.Customer.LoyaltyPoints);

                // Update available tickets
                booking.Event.AvailableTickets -= booking.Quantity;
                _logger.LogInformation("📊 EVENT TICKETS UPDATED:");
                _logger.LogInformation("   Tickets Sold: {Quantity}", booking.Quantity);
                _logger.LogInformation("   Remaining: {Remaining}", booking.Event.AvailableTickets);

                // SAVE ALL CHANGES
                _logger.LogInformation("💾 SAVING ALL CHANGES TO DATABASE...");
                var savedCount = await _context.SaveChangesAsync();
                _logger.LogInformation("✅ DATABASE UPDATED - {Count} records saved", savedCount);

                TempData["SuccessMessage"] = $"Payment successful! Booking confirmed: {booking.BookingReference}";

                _logger.LogInformation("🎉 PAYMENT COMPLETED SUCCESSFULLY!");
                _logger.LogInformation("🔀 REDIRECTING TO SUCCESS PAGE - PaymentId: {PaymentId}", payment.Id);
                _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
                _logger.LogInformation("║          PAYMENT PROCESS COMPLETED                        ║");
                _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");

                return RedirectToAction("Success", new { id = payment.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError("╔═══════════════════════════════════════════════════════════╗");
                _logger.LogError("║          CRITICAL ERROR IN PAYMENT PROCESS                ║");
                _logger.LogError("╚═══════════════════════════════════════════════════════════╝");
                _logger.LogError("❌ Exception Type: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("❌ Error Message: {Message}", ex.Message);
                _logger.LogError("❌ Stack Trace: {StackTrace}", ex.StackTrace);
                if (ex.InnerException != null)
                {
                    _logger.LogError("❌ Inner Exception: {InnerException}", ex.InnerException.Message);
                }

                TempData["ErrorMessage"] = $"Payment failed: {ex.Message}";
                return RedirectToAction("Checkout", "Booking", new { id = model.BookingId });
            }
        }

        public async Task<IActionResult> Success(int id)
        {
            _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║          SUCCESS PAGE REQUESTED                           ║");
            _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");
            _logger.LogInformation("📄 Payment ID: {PaymentId}", id);

            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogWarning("❌ User not logged in on success page");
                    return RedirectToAction("Login", "Account");
                }

                var customerId = int.Parse(userIdString);
                _logger.LogInformation("✅ Customer ID: {CustomerId}", customerId);

                _logger.LogInformation("📦 FETCHING PAYMENT DATA...");
                var payment = await _context.Payments
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Event)
                            .ThenInclude(e => e.Venue)
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Tickets)
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Customer)
                    .FirstOrDefaultAsync(p => p.Id == id && p.Booking.CustomerId == customerId);

                if (payment == null)
                {
                    _logger.LogError("❌ PAYMENT NOT FOUND - PaymentId: {PaymentId}", id);
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                _logger.LogInformation("✅ PAYMENT DATA LOADED:");
                _logger.LogInformation("   Booking: {BookingId}", payment.Booking.Id);
                _logger.LogInformation("   Reference: {Reference}", payment.Booking.BookingReference);
                _logger.LogInformation("   Event: {Event}", payment.Booking.Event.Title);
                _logger.LogInformation("   Tickets: {Count}", payment.Booking.Tickets.Count);

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
                    LoyaltyPointsEarned = (int)payment.Amount,
                    CustomerEmail = payment.Booking.Customer.Email
                };

                _logger.LogInformation("✅ SUCCESS VIEW MODEL CREATED - RENDERING PAGE");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ ERROR IN SUCCESS PAGE: {Message}", ex.Message);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
                TempData["ErrorMessage"] = "Unable to load confirmation page.";
                return RedirectToAction("MyBookings", "Booking");
            }
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

                var query = _context.Payments
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Event)
                    .Where(p => p.Booking.CustomerId == customerId);

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

                var allPayments = await query.ToListAsync();
                var totalPayments = allPayments.Count;
                var successfulPayments = allPayments.Count(p => p.Status == PaymentStatus.Completed);
                var failedPayments = allPayments.Count(p => p.Status == PaymentStatus.Failed);
                var totalAmountPaid = allPayments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);

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