using EventHub.Models.Entities;
using System;
using System.Collections.Generic;

namespace EventHub.Models.ViewModels
{
    /// <summary>
    /// ViewModel for My Bookings page
    /// </summary>
    public class MyBookingsViewModel
    {
        public List<BookingDisplayDto> Bookings { get; set; } = new List<BookingDisplayDto>();

        // Statistics
        public int TotalBookings { get; set; }
        public int UpcomingCount { get; set; }
        public int CompletedCount { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? DateFilter { get; set; }
        public string? SortBy { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
    }

    /// <summary>
    /// DTO for displaying booking information
    /// </summary>
    public class BookingDisplayDto
    {
        public int Id { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public BookingStatus Status { get; set; }

        // Event Information
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventCategory { get; set; } = string.Empty;
        public string? EventImageUrl { get; set; }

        // Venue Information
        public string VenueName { get; set; } = string.Empty;
        public string VenueLocation { get; set; } = string.Empty;

        // Payment Information
        public string? PaymentStatus { get; set; }
        public string? PaymentMethod { get; set; }

        // Ticket Information
        public int TicketCount { get; set; }
        public bool HasTickets { get; set; }

        // Helper Properties
        public string FormattedBookingDate => BookingDate.ToString("MMM dd, yyyy");
        public string FormattedEventDate => EventDate.ToString("MMM dd, yyyy 'at' hh:mm tt");
        public string FormattedAmount => TotalAmount.ToString("C");
        public bool IsPastEvent => EventDate < DateTime.UtcNow;
        public bool IsUpcoming => EventDate > DateTime.UtcNow;
        public bool CanCancel => Status == BookingStatus.Confirmed &&
                                 EventDate > DateTime.UtcNow.AddDays(7);
    }
}