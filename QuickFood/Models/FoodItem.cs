using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoodFrenzy.Models
{
    public class FoodItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10000.00)]
        public decimal Price { get; set; }

        [Range(0.0, 5.0)]
        public double Rating { get; set; } = 0.0;

        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        // Navigation properties
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}