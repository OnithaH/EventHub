namespace EventHub.Models.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        public int UpcomingEventsCount { get; set; }
        public int TotalTicketsPurchased { get; set; }
        public decimal TotalAmountSpent { get; set; }

        public List<UpcomingEventDto> UpcomingEvents { get; set; } = new();
        public List<RecentBookingDto> RecentBookings { get; set; } = new();
        public List<RecommendedEventDto> RecommendedEvents { get; set; } = new();

        // Calculated properties for loyalty program
        public int PointsToNextReward => 100 - (LoyaltyPoints % 100);
        public double ProgressPercentage => ((LoyaltyPoints % 100) / 100.0) * 100;
        public bool CanRedeemPoints => LoyaltyPoints >= 100;
    }

    public class UpcomingEventDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string VenueLocation { get; set; } = string.Empty;
        public decimal TicketPrice { get; set; }
        public string? ImageUrl { get; set; }
        public int BookingId { get; set; }
        public int TicketQuantity { get; set; }
        public string BookingStatus { get; set; } = string.Empty;

        public string FormattedDate => EventDate.ToString("MMM dd, yyyy");
    }

    public class RecentBookingDto
    {
        public int BookingId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string EventCategory { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime EventDate { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? EventImageUrl { get; set; }

        public string FormattedBookingDate => BookingDate.ToString("MMM dd, yyyy");
        public string FormattedAmount => TotalAmount.ToString("C");
        public string StatusBadgeClass => Status.ToLower() switch
        {
            "confirmed" => "bg-success",
            "pending" => "bg-warning",
            "cancelled" => "bg-danger",
            "completed" => "bg-info",
            _ => "bg-secondary"
        };
    }

    public class RecommendedEventDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string VenueLocation { get; set; } = string.Empty;
        public decimal TicketPrice { get; set; }
        public int AvailableTickets { get; set; }
        public string? ImageUrl { get; set; }

        public string FormattedDate => EventDate.ToString("MMM dd, yyyy");
        public string FormattedPrice => TicketPrice.ToString("C");
    }
}