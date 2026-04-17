
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodFrenzy.Models;

namespace FoodFrenzy.Models.Interfaces
{
    public interface IOrderRepository
    {
       
        Task<Order> CreateOrderAsync(Order order, List<CartItem> cartItems);
        Task<Order> GetOrderByIdAsync(int orderId);
        Task<Order> GetOrderByTrackingNumberAsync(string trackingNumber);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task<Order> GetOrderWithItemsAsync(int orderId);

        // Status management
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);

        // Number generation
        Task<string> GenerateTrackingNumberAsync();
        Task<string> GenerateOrderNumberAsync();

        // Bulk retrieval
        IEnumerable<Order> GetOrdersByDateRange(DateTime startDate, DateTime endDate);
        IEnumerable<Order> GetAllOrders();
        public Order GetOrderById(int orderId);
    }
}