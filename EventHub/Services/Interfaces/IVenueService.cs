using EventHub.Models.Entities;

namespace EventHub.Services.Interfaces
{
    public interface IVenueService
    {
        Task<IEnumerable<Venue>> GetAllVenuesAsync();
        Task<Venue?> GetVenueByIdAsync(int id);
        Task<Venue> CreateVenueAsync(Venue venue);
        Task<bool> UpdateVenueAsync(Venue venue);
        Task<bool> DeleteVenueAsync(int id);
        Task<IEnumerable<Venue>> SearchVenuesAsync(string? searchTerm = null);
        Task<int> GetEventCountByVenueAsync(int venueId);
        Task<bool> VenueHasActiveEventsAsync(int venueId);
    }
}