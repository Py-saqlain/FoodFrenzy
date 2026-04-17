namespace FoodFrenzy.Models
{
    public class OrderHistory
    {
        public string OrderId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Items { get; set; } = string.Empty;
    }
}