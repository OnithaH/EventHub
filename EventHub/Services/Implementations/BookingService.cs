using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Booking> CreateBookingAsync(Booking booking)
        {
            booking.BookingDate = DateTime.UtcNow;
            booking.BookingReference = await GenerateBookingReferenceAsync();
            booking.Status = BookingStatus.Pending;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task<Booking?> GetBookingByIdAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.Payment)
                .Include(b => b.Tickets)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Booking>> GetBookingsByCustomerAsync(int customerId)
        {
            return await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.Payment)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetBookingsByEventAsync(int eventId)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Payment)
                .Where(b => b.EventId == eventId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status)
        {
            var booking = await GetBookingByIdAsync(bookingId);
            if (booking == null) return false;

            booking.Status = status;
            _context.Bookings.Update(booking);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            var booking = await GetBookingByIdAsync(bookingId);
            if (booking == null) return false;

            // Update booking status
            booking.Status = BookingStatus.Cancelled;

            // Return tickets to available pool
            var eventModel = await _context.Events.FindAsync(booking.EventId);
            if (eventModel != null)
            {
                eventModel.AvailableTickets += booking.Quantity;
                _context.Events.Update(eventModel);
            }

            _context.Bookings.Update(booking);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<string> GenerateBookingReferenceAsync()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            var reference = $"BK{timestamp}{random}";

            // Ensure uniqueness
            while (await _context.Bookings.AnyAsync(b => b.BookingReference == reference))
            {
                random = new Random().Next(1000, 9999);
                reference = $"BK{timestamp}{random}";
            }

            return reference;
        }

        public async Task<decimal> CalculateTotalAmountAsync(int eventId, int quantity, int? discountId = null)
        {
            var eventModel = await _context.Events.FindAsync(eventId);
            if (eventModel == null) return 0;

            var totalAmount = eventModel.TicketPrice * quantity;

            if (discountId.HasValue)
            {
                var discount = await _context.Discounts
                    .FirstOrDefaultAsync(d => d.Id == discountId.Value &&
                                             d.IsActive &&
                                             d.ValidFrom <= DateTime.UtcNow &&
                                             d.ValidTo >= DateTime.UtcNow);

                if (discount != null)
                {
                    var discountAmount = totalAmount * (discount.Percentage / 100);
                    totalAmount -= discountAmount;
                }
            }

            return totalAmount;
        }
    }
}