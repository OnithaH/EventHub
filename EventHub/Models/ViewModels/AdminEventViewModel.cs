using EventHub.Models.Entities;

namespace EventHub.Models.ViewModels
{
    public class AdminEventViewModel
    {
        public Event Event { get; set; } = new Event();
        public string OrganizerName { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TicketsSold { get; set; }
    }

    public class AdminEventsListViewModel
    {
        public IEnumerable<AdminEventViewModel> Events { get; set; } = new List<AdminEventViewModel>();
        public EventFilterViewModel Filters { get; set; } = new EventFilterViewModel();
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int InactiveEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int PastEvents { get; set; }
    }
}