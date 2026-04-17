using System.ComponentModel.DataAnnotations;

namespace FoodFrenzy.Models
{
    public class OrderViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string CustomerName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string CustomerEmail { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string CustomerPhone { get; set; }

        [Required]
        [Display(Name = "Delivery Address")]
        public string DeliveryAddress { get; set; }

        [Required]
        [Display(Name = "City")]
        public string City { get; set; }

        [Required]
        [Display(Name = "ZIP Code")]
        public string ZipCode { get; set; }

        [Display(Name = "Delivery Instructions (Optional)")]
        public string DeliveryInstructions { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

       
    }
}
