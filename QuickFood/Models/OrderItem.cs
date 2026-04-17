using System.ComponentModel.DataAnnotations;

namespace FoodFrenzy.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int FoodItemId { get; set; }

        // Add this navigation property
        public FoodItem FoodItem { get; set; }

        public string FoodItemName { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
    }
}