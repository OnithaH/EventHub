using EventHub.Models.Entities;
using System;
using System.Collections.Generic;

namespace EventHub.Models.ViewModels
{
    public class MyTicketsViewModel
    {
        public List<TicketDisplayDto> Tickets { get; set; } = new List<TicketDisplayDto>();

        // Statistics
        public int TotalTickets { get; set; }
        public int ActiveTickets { get; set; }
        public int UsedTickets { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? DateFilter { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
    }

    public class TicketDisplayDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string QRCode { get; set; } = string.Empty;
        public string QRCodeBase64 { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime? UsedDate { get; set; }

        // Booking Info
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;

        // Event Info
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventCategory { get; set; } = string.Empty;

        // Venue Info
        public string VenueName { get; set; } = string.Empty;
        public string VenueLocation { get; set; } = string.Empty;

        // Customer Info
        public string CustomerName { get; set; } = string.Empty;

        // Helper Properties
        public bool IsActive => Status == TicketStatus.Active;
        public bool IsUsed => Status == TicketStatus.Used;
        public bool IsExpired => Status == TicketStatus.Expired;
    }
}