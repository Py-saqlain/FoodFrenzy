using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoodFrenzy.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public decimal Subtotal { get; set; }

        [Required]
        public decimal DeliveryFee { get; set; }

        [Required]
        public decimal ServiceFee { get; set; }

        [Required]
        public decimal Tax { get; set; }

        [Required]
        public decimal Total { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string DeliveryInstructions { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;
        public string DeliveryMethod { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Pending";
        public string TrackingNumber { get; set; } = string.Empty;

        // Navigation property
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // User relationship
        public ApplicationUser User { get; set; }
        public decimal TotalAmount { get; internal set; }
    }
}