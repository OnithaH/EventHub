namespace EventHub.Models.ViewModels
{
    public class SystemReportsViewModel
    {
        // Revenue Analytics
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisYear { get; set; }
        public List<RevenueByEventDto> TopRevenueEvents { get; set; } = new();
        public List<MonthlyRevenueDto> MonthlyRevenueTrend { get; set; } = new();

        // User Analytics
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrganizers { get; set; }
        public int TotalAdmins { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }

        // Event Analytics
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int InactiveEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int PastEvents { get; set; }
        public List<EventByCategoryDto> EventsByCategory { get; set; } = new();
        public List<PopularEventDto> MostPopularEvents { get; set; } = new();

        // Booking Analytics
        public int TotalBookings { get; set; }
        public int BookingsThisMonth { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int CompletedBookings { get; set; }
        public List<AdminRecentBookingDto> RecentBookings { get; set; } = new(); // ← RENAMED

        // Date Filters
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class RevenueByEventDto
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string OrganizerName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TicketsSold { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Year { get; set; }
    }

    public class EventByCategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public int EventCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PopularEventDto
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public int TicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime EventDate { get; set; }
    }

    // ← RENAMED from RecentBookingDto to AdminRecentBookingDto
    public class AdminRecentBookingDto
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}