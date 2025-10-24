using EventHub.Models.Entities;
namespace EventHub.Models.ViewModels
{
    public class AdminVenueViewModel
    {
        public Venue Venue { get; set; } = new Venue();
        public int EventCount { get; set; }
        public int ActiveEventCount { get; set; }
        public int UpcomingEventCount { get; set; }
        public bool HasActiveEvents { get; set; }
    }

    public class AdminVenuesListViewModel
    {
        public IEnumerable<AdminVenueViewModel> Venues { get; set; } = new List<AdminVenueViewModel>();
        public string? SearchTerm { get; set; }
        public int TotalVenues { get; set; }
        public int VenuesWithEvents { get; set; }
    }
}