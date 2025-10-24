using EventHub.Models.Entities;
namespace EventHub.Models.ViewModels
{
    public class EventFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public string? OrganizerName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; } = "date_desc";
    }
}