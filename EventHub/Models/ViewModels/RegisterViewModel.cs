using System.ComponentModel.DataAnnotations;
using EventHub.Models.Entities;

namespace EventHub.Models.ViewModels
{
    public class RegisterViewModel
    {
        // 🔧 FIX: Added detailed error messages
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        // 🔧 FIX: Removed overly strict regex - just check length
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        // 🔧 FIX: Use Compare attribute to validate matching
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Please select a role")]
        public UserRole Role { get; set; } = UserRole.Customer;

        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
        public string? Company { get; set; }

        // Additional profile fields
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(500)]
        public string? Interests { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? OrganizationType { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = false;
        public bool MarketingEmails { get; set; } = true;
    }
}