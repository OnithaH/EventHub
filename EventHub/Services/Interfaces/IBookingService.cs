using EventHub.Models.Entities;

namespace EventHub.Services.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Booking booking);
        Task<Booking?> GetBookingByIdAsync(int id);
        Task<IEnumerable<Booking>> GetBookingsByCustomerAsync(int customerId);
        Task<IEnumerable<Booking>> GetBookingsByEventAsync(int eventId);
        Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status);
        Task<bool> CancelBookingAsync(int bookingId);
        Task<string> GenerateBookingReferenceAsync();
        Task<decimal> CalculateTotalAmountAsync(int eventId, int quantity, int? discountId = null);
    }
}