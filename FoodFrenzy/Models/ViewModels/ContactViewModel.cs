using System.ComponentModel.DataAnnotations;

namespace FoodFrenzy.Models.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [StringLength(5000, ErrorMessage = "Message cannot exceed 5000 characters")]
        [Display(Name = "Your Message")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Subscribe to newsletter")]
        public bool SubscribeToNewsletter { get; set; }

        // Helper property
        public string FullName => $"{FirstName} {LastName}";
    }
}