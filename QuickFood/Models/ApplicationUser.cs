using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace FoodFrenzy.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? imgpath { get; set; }
        public string Status { get; set; } = "pending";

        // Navigation properties
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}