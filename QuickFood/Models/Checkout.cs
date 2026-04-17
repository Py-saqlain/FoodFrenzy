using System.Collections.Generic;

namespace FoodFrenzy.Models
{
    public class Checkout
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public List<CartItem> CartItems { get; set; }
    }
}