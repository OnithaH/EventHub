using System.ComponentModel.DataAnnotations;
using EventHub.Models.Entities;

namespace EventHub.Models.ViewModels
{
    public class RegisterViewModel
    {
        // Existing fields - DO NOT CHANGE
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Please select a role")]
        public UserRole Role { get; set; } = UserRole.Customer;

        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
        public string? Company { get; set; }

        // NEW FIELDS - Additional profile information (all optional to maintain compatibility)
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        // Customer specific
        [StringLength(500)]
        public string? Interests { get; set; }

        // Organizer specific  
        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? OrganizationType { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        // Communication preferences (default to true for backwards compatibility)
        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = false;
        public bool MarketingEmails { get; set; } = true;

        // Helper method to split name into first/last for display purposes
        public (string FirstName, string LastName) GetSplitName()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return ("", "");

            var parts = Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return (parts[0], "");
            if (parts.Length == 2)
                return (parts[0], parts[1]);

            // If more than 2 parts, first is first name, rest is last name
            return (parts[0], string.Join(" ", parts.Skip(1)));
        }
    }
}