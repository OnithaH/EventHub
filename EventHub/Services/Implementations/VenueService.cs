using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Services.Implementations
{
    public class VenueService : IVenueService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VenueService> _logger;

        public VenueService(ApplicationDbContext context, ILogger<VenueService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Venue>> GetAllVenuesAsync()
        {
            try
            {
                return await _context.Venues
                    .Include(v => v.Events)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all venues");
                throw;
            }
        }

        public async Task<Venue?> GetVenueByIdAsync(int id)
        {
            try
            {
                return await _context.Venues
                    .Include(v => v.Events)
                    .FirstOrDefaultAsync(v => v.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving venue with ID {VenueId}", id);
                throw;
            }
        }

        public async Task<Venue> CreateVenueAsync(Venue venue)
        {
            try
            {
                venue.CreatedAt = DateTime.UtcNow;
                _context.Venues.Add(venue);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Venue created successfully: {VenueName}", venue.Name);
                return venue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating venue");
                throw;
            }
        }

        public async Task<bool> UpdateVenueAsync(Venue venue)
        {
            try
            {
                _context.Venues.Update(venue);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Venue updated successfully: {VenueName}", venue.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating venue with ID {VenueId}", venue.Id);
                return false;
            }
        }

        public async Task<bool> DeleteVenueAsync(int id)
        {
            try
            {
                var venue = await _context.Venues.FindAsync(id);
                if (venue == null)
                {
                    return false;
                }

                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Venue deleted successfully: {VenueName}", venue.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting venue with ID {VenueId}", id);
                return false;
            }
        }

        public async Task<IEnumerable<Venue>> SearchVenuesAsync(string? searchTerm = null)
        {
            try
            {
                var query = _context.Venues.Include(v => v.Events).AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(v =>
                        v.Name.Contains(searchTerm) ||
                        v.Location.Contains(searchTerm) ||
                        (v.Address != null && v.Address.Contains(searchTerm)));
                }

                return await query.OrderBy(v => v.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching venues");
                throw;
            }
        }

        public async Task<int> GetEventCountByVenueAsync(int venueId)
        {
            try
            {
                return await _context.Events
                    .Where(e => e.VenueId == venueId)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event count for venue {VenueId}", venueId);
                return 0;
            }
        }

        public async Task<bool> VenueHasActiveEventsAsync(int venueId)
        {
            try
            {
                return await _context.Events
                    .AnyAsync(e => e.VenueId == venueId && e.IsActive && e.EventDate >= DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active events for venue {VenueId}", venueId);
                return false;
            }
        }
    }
}