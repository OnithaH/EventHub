using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Services.Implementations
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;

        public EventService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Where(e => e.IsActive && e.EventDate >= DateTime.UtcNow)
                .OrderBy(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Event> CreateEventAsync(Event eventModel)
        {
            eventModel.CreatedAt = DateTime.UtcNow;
            eventModel.TotalTickets = eventModel.AvailableTickets;

            _context.Events.Add(eventModel);
            await _context.SaveChangesAsync();
            return eventModel;
        }

        public async Task<bool> UpdateEventAsync(Event eventModel)
        {
            _context.Events.Update(eventModel);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            var eventModel = await GetEventByIdAsync(id);
            if (eventModel == null) return false;

            eventModel.IsActive = false; // Soft delete
            return await UpdateEventAsync(eventModel);
        }

        public async Task<IEnumerable<Event>> SearchEventsAsync(string? category = null, DateTime? date = null, string? location = null)
        {
            var query = _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Where(e => e.IsActive);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category.ToLower().Contains(category.ToLower()));
            }

            if (date.HasValue)
            {
                query = query.Where(e => e.EventDate.Date == date.Value.Date);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(e => e.Venue.Location.ToLower().Contains(location.ToLower()));
            }

            return await query.OrderBy(e => e.EventDate).ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(int organizerId)
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateAvailableTicketsAsync(int eventId, int ticketsSold)
        {
            var eventModel = await GetEventByIdAsync(eventId);
            if (eventModel == null) return false;

            eventModel.AvailableTickets -= ticketsSold;
            return await UpdateEventAsync(eventModel);
        }
    }
}