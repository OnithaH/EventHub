using System.ComponentModel.DataAnnotations;
using EventHub.Models.Entities;

namespace EventHub.Models.ViewModels
{
    public class EditProfileViewModel
    {
        // Basic Information (Both Roles)
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [StringLength(20)]
        public string? Phone { get; set; }

        // Personal Information (Mainly for Customers)
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(500)]
        public string? Interests { get; set; }

        // Company Information (For Organizers)
        [StringLength(200)]
        public string? Company { get; set; }

        [StringLength(50)]
        public string? OrganizationType { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        // Notification Preferences (Both Roles)
        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = false;
        public bool MarketingEmails { get; set; } = true;

        // Password Change (Optional)
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match")]
        public string? ConfirmNewPassword { get; set; }

        // Read-only properties
        public UserRole Role { get; set; }
        public int LoyaltyPoints { get; set; }
        public DateTime CreatedAt { get; set; }

        // Helper method to check if password change is requested
        public bool IsPasswordChangeRequested()
        {
            return !string.IsNullOrWhiteSpace(CurrentPassword) ||
                   !string.IsNullOrWhiteSpace(NewPassword) ||
                   !string.IsNullOrWhiteSpace(ConfirmNewPassword);
        }
    }
}