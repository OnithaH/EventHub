using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHub.Models.Entities
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public TimeSpan EventTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TicketPrice { get; set; }

        [Required]
        public int AvailableTickets { get; set; }

        public int TotalTickets { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        [Required]
        public int VenueId { get; set; }

        [Required]
        public int OrganizerId { get; set; }

        [ForeignKey("VenueId")]
        public virtual Venue? Venue { get; set; }

        [ForeignKey("OrganizerId")]
        public virtual User? Organizer { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}