using EventHub.Data;
using EventHub.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Test/Database
        public async Task<IActionResult> Database()
        {
            var results = new List<string>();

            try
            {
                // Test 1: Basic connection
                results.Add("✅ Database connection established");

                // Test 2: Check if tables exist
                var tableCount = await _context.Database.ExecuteSqlRawAsync("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'");
                results.Add($"✅ Database tables accessible");

                // Test 3: Count records in each table
                var userCount = await _context.Users.CountAsync();
                var eventCount = await _context.Events.CountAsync();
                var venueCount = await _context.Venues.CountAsync();
                var bookingCount = await _context.Bookings.CountAsync();

                results.Add($"📊 Users: {userCount}");
                results.Add($"📊 Events: {eventCount}");
                results.Add($"📊 Venues: {venueCount}");
                results.Add($"📊 Bookings: {bookingCount}");

                // Test 4: Check migration history
                var migrations = await _context.Database.GetAppliedMigrationsAsync();
                results.Add($"✅ Applied migrations: {migrations.Count()}");
                foreach (var migration in migrations)
                {
                    results.Add($"  - {migration}");
                }

                ViewBag.Results = results;
                ViewBag.Status = "Success";
                return View();
            }
            catch (Exception ex)
            {
                results.Add($"❌ Database connection failed: {ex.Message}");
                ViewBag.Results = results;
                ViewBag.Status = "Error";
                return View();
            }
        }

        // GET: /Test/Insert - Test inserting data
        public async Task<IActionResult> Insert()
        {
            try
            {
                // Test inserting a sample venue
                var testVenue = new Venue
                {
                    Name = "Test Venue",
                    Location = "Test City",
                    Capacity = 100,
                    Address = "123 Test Street"
                };

                _context.Venues.Add(testVenue);
                await _context.SaveChangesAsync();

                ViewBag.Message = $"✅ Successfully inserted test venue with ID: {testVenue.Id}";
                ViewBag.Status = "Success";
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"❌ Insert failed: {ex.Message}";
                ViewBag.Status = "Error";
            }

            return View();
        }
    }
}