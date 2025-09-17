using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHub.Models.Entities
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string QRCode { get; set; } = string.Empty;

        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public TicketStatus Status { get; set; } = TicketStatus.Active;

        [MaxLength(20)]
        public string? SeatNumber { get; set; }

        public DateTime? UsedDate { get; set; }

        [MaxLength(50)]
        public string TicketNumber { get; set; } = string.Empty;

        // Foreign Key
        [Required]
        public int BookingId { get; set; }

        // Navigation property
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; } = null!;
    }

    public enum TicketStatus
    {
        Active = 0,
        Used = 1,
        Cancelled = 2,
        Expired = 3
    }
}