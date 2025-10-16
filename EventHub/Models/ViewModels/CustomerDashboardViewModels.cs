using System;
using System.Collections.Generic;

namespace EventHub.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Customer Dashboard
    /// Contains all data needed to display the dashboard
    /// </summary>
    public class CustomerDashboardViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }

        // Statistics
        public int UpcomingEventsCount { get; set; }
        public int TotalTicketsPurchased { get; set; }
        public decimal TotalAmountSpent { get; set; }

        // Collections
        public List<UpcomingEventDto> UpcomingEvents { get; set; } = new List<UpcomingEventDto>();
        public List<RecentBookingDto> RecentBookings { get; set; } = new List<RecentBookingDto>();
        public List<RecommendedEventDto> RecommendedEvents { get; set; } = new List<RecommendedEventDto>();
    }

    /// <summary>
    /// DTO for Upcoming Events display
    /// </summary>
    public class UpcomingEventDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int TicketsBooked { get; set; }

        public string FormattedDate => EventDate.ToString("MMM dd, yyyy 'at' hh:mm tt");
    }

    /// <summary>
    /// DTO for Recent Booking Activity
    /// </summary>
    public class RecentBookingDto
    {
        public int BookingId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? EventImageUrl { get; set; }

        public string FormattedBookingDate => BookingDate.ToString("MMM dd, yyyy");
        public string FormattedAmount => TotalAmount.ToString("C");
        public string StatusBadgeClass => Status.ToLower() switch
        {
            "confirmed" => "badge-confirmed",
            "pending" => "badge-pending",
            "cancelled" => "badge-danger",
            "completed" => "badge-info",
            _ => "badge-secondary"
        };
    }

    /// <summary>
    /// DTO for Recommended Events
    /// </summary>
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