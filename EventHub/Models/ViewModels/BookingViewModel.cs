using System.ComponentModel.DataAnnotations;

namespace EventHub.Models.ViewModels
{
    public class BookingViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public decimal TicketPrice { get; set; }
        public int AvailableTickets { get; set; }

        [Required(ErrorMessage = "Please specify the number of tickets")]
        [Range(1, 10, ErrorMessage = "You can book between 1 and 10 tickets")]
        public int Quantity { get; set; } = 1;

        [StringLength(20, ErrorMessage = "Discount code cannot exceed 20 characters")]
        public string? DiscountCode { get; set; }

        public decimal TotalAmount { get; set; }
        public bool UseLoyltyPoints { get; set; } = false;
    }
}