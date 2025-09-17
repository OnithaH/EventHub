using EventHub.Models.Entities;

namespace EventHub.Services.Interfaces
{
    public interface IEventService
    {
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventAsync(Event eventModel);
        Task<bool> UpdateEventAsync(Event eventModel);
        Task<bool> DeleteEventAsync(int id);
        Task<IEnumerable<Event>> SearchEventsAsync(string? category = null, DateTime? date = null, string? location = null);
        Task<IEnumerable<Event>> GetEventsByOrganizerAsync(int organizerId);
        Task<bool> UpdateAvailableTicketsAsync(int eventId, int ticketsSold);
    }
}