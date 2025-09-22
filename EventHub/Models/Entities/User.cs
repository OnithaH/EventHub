using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHub.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public int LoyaltyPoints { get; set; } = 0;

        [MaxLength(200)]
        public string? Company { get; set; } // For organizers only

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Add these NEW properties to your existing User class:

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(500)]
        public string? Interests { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(50)]
        public string? OrganizationType { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = false;
        public bool MarketingEmails { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    public enum UserRole
    {
        Admin = 0,
        Customer = 1,
        Organizer = 2
    }
}