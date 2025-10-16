using EventHub.Models.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventHub.Models.ViewModels
{
    // ViewModel for Payment History Page
    public class PaymentHistoryViewModel
    {
        public List<PaymentDisplayDto> Payments { get; set; } = new List<PaymentDisplayDto>();
        public int TotalPayments { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public decimal TotalAmountPaid { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public string? DateFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
    }

    public class PaymentDisplayDto
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
    }

    // ViewModel for Payment Success Page
    public class PaymentSuccessViewModel
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public decimal AmountPaid { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public int TicketCount { get; set; }
        public int LoyaltyPointsEarned { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
    }

    // ViewModel for Checkout Page
    public class CheckoutViewModel
    {
        public int BookingId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string? EventImageUrl { get; set; }

        public decimal TicketPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LoyaltyPointsDiscount { get; set; }
        public decimal Amount { get; set; }

        // Payment Info
        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethod { get; set; } = string.Empty;

        // Billing Info
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        // Optional
        public string? DiscountCode { get; set; }
        public bool UseLoyaltyPoints { get; set; }
        public int AvailableLoyaltyPoints { get; set; }
        public int LoyaltyPointsUsed { get; set; }
        public int PointsToEarn { get; set; }
    }
}