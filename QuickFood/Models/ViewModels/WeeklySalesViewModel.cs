
namespace FoodFrenzy.Models.ViewModels
{
    public class WeeklySalesViewModel
    {
        public List<DailySalesData> WeeklyData { get; set; }
        public List<TopProduct> TopProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public string BestDay { get; set; }
    }

    public class DailySalesData
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class TopProduct
    {
        public string Name { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public int QuantitySold { get; set; }
    }
}