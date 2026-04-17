using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FoodFrenzy.Models.Interfaces;

namespace FoodFrenzy.Models.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(IConfiguration config, ILogger<OrderRepository> logger)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task<Order> CreateOrderAsync(Order order, List<CartItem> cartItems)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();

            using var transaction = await con.BeginTransactionAsync();

            try
            {
                // Generate order number if not provided
                if (string.IsNullOrEmpty(order.OrderNumber))
                {
                    order.OrderNumber = await GenerateOrderNumberAsync();
                }

                // Generate tracking number
                order.TrackingNumber = await GenerateTrackingNumberAsync();
                order.OrderDate = DateTime.UtcNow;

                // Insert order
                var orderSql = @"
                    INSERT INTO Orders (
                        UserId, OrderNumber, Subtotal, DeliveryFee, ServiceFee, Tax, Total,
                        CustomerName, CustomerEmail, CustomerPhone, DeliveryAddress, City, ZipCode,
                        DeliveryInstructions, PaymentMethod, DeliveryMethod, OrderDate, Status, TrackingNumber
                    )
                    OUTPUT INSERTED.Id
                    VALUES (
                        @UserId, @OrderNumber, @Subtotal, @DeliveryFee, @ServiceFee, @Tax, @Total,
                        @CustomerName, @CustomerEmail, @CustomerPhone, @DeliveryAddress, @City, @ZipCode,
                        @DeliveryInstructions, @PaymentMethod, @DeliveryMethod, @OrderDate, @Status, @TrackingNumber
                    )";

                var orderId = await con.ExecuteScalarAsync<int>(orderSql, order, transaction);
                order.Id = orderId;

                // Insert order items
                var itemSql = @"
                    INSERT INTO OrderItems (OrderId, FoodItemId, FoodItemName, Price, Quantity, ImageUrl)
                    VALUES (@OrderId, @FoodItemId, @FoodItemName, @Price, @Quantity, @ImageUrl)";

                foreach (var cartItem in cartItems)
                {
                    await con.ExecuteAsync(itemSql, new
                    {
                        OrderId = orderId,
                        FoodItemId = cartItem.FoodItemId,
                        FoodItemName = cartItem.Name,
                        Price = cartItem.Price,
                        Quantity = cartItem.Quantity,
                        ImageUrl = cartItem.ImageUrl
                    }, transaction);
                }

                await transaction.CommitAsync();
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        public IEnumerable<Order> GetOrdersByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                // Get orders within date range
                const string ordersSql = @"
                    SELECT o.*, 
                           oi.Id, oi.FoodItemId, oi.FoodItemName, oi.Price, oi.Quantity, oi.ImageUrl
                    FROM Orders o
                    LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
                    WHERE o.OrderDate >= @StartDate AND o.OrderDate <= @EndDate
                    ORDER BY o.OrderDate DESC";

                var orderDictionary = new Dictionary<int, Order>();

                var result = connection.Query<Order, OrderItem, Order>(
                    ordersSql,
                    (order, orderItem) =>
                    {
                        if (!orderDictionary.TryGetValue(order.Id, out var orderEntry))
                        {
                            orderEntry = order;
                            orderEntry.OrderItems = new List<OrderItem>();
                            orderDictionary.Add(orderEntry.Id, orderEntry);
                        }

                        if (orderItem != null)
                        {
                            orderEntry.OrderItems.Add(orderItem);
                        }

                        return orderEntry;
                    },
                    new { StartDate = startDate, EndDate = endDate },
                    splitOn: "Id"
                );

                return orderDictionary.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by date range");
                return new List<Order>();
            }
        }

        public IEnumerable<Order> GetAllOrders()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                const string ordersSql = @"
                    SELECT o.*, 
                           oi.Id, oi.FoodItemId, oi.FoodItemName, oi.Price, oi.Quantity, oi.ImageUrl
                    FROM Orders o
                    LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
                    ORDER BY o.OrderDate DESC";

                var orderDictionary = new Dictionary<int, Order>();

                var result = connection.Query<Order, OrderItem, Order>(
                    ordersSql,
                    (order, orderItem) =>
                    {
                        if (!orderDictionary.TryGetValue(order.Id, out var orderEntry))
                        {
                            orderEntry = order;
                            orderEntry.OrderItems = new List<OrderItem>();
                            orderDictionary.Add(orderEntry.Id, orderEntry);
                        }

                        if (orderItem != null)
                        {
                            orderEntry.OrderItems.Add(orderItem);
                        }

                        return orderEntry;
                    },
                    splitOn: "Id"
                );

                _logger.LogInformation($"Retrieved {orderDictionary.Count} orders from database");
                return orderDictionary.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all orders");
                return new List<Order>();
            }
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.OpenAsync();

                var orderSql = @"
                    SELECT o.*, 
                           oi.Id, oi.FoodItemId, oi.FoodItemName, oi.Price, oi.Quantity, oi.ImageUrl
                    FROM Orders o
                    LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
                    WHERE o.Id = @OrderId";

                var orderDictionary = new Dictionary<int, Order>();

                var result = await con.QueryAsync<Order, OrderItem, Order>(
                    orderSql,
                    (order, orderItem) =>
                    {
                        if (!orderDictionary.TryGetValue(order.Id, out var orderEntry))
                        {
                            orderEntry = order;
                            orderEntry.OrderItems = new List<OrderItem>();
                            orderDictionary.Add(orderEntry.Id, orderEntry);
                        }

                        if (orderItem != null)
                        {
                            orderEntry.OrderItems.Add(orderItem);
                        }

                        return orderEntry;
                    },
                    new { OrderId = orderId },
                    splitOn: "Id"
                );

                var order = result.FirstOrDefault();

                if (order == null)
                {
                    _logger.LogWarning($"Order with ID {orderId} not found in database");
                }
                else
                {
                    _logger.LogInformation($"Found order ID {orderId}: #{order.OrderNumber}");
                }

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting order by ID: {orderId}");
                return null;
            }
        }

        public async Task<Order> GetOrderByTrackingNumberAsync(string trackingNumber)
        {
            using var con = new SqlConnection(_connectionString);

            var orderSql = @"
                SELECT o.*, 
                       oi.Id, oi.FoodItemId, oi.FoodItemName, oi.Price, oi.Quantity, oi.ImageUrl
                FROM Orders o
                LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
                WHERE o.TrackingNumber = @TrackingNumber";

            var orderDictionary = new Dictionary<int, Order>();

            var result = await con.QueryAsync<Order, OrderItem, Order>(
                orderSql,
                (order, orderItem) =>
                {
                    if (!orderDictionary.TryGetValue(order.Id, out var orderEntry))
                    {
                        orderEntry = order;
                        orderEntry.OrderItems = new List<OrderItem>();
                        orderDictionary.Add(orderEntry.Id, orderEntry);
                    }

                    if (orderItem != null)
                    {
                        orderEntry.OrderItems.Add(orderItem);
                    }

                    return orderEntry;
                },
                new { TrackingNumber = trackingNumber },
                splitOn: "Id"
            );

            return result.FirstOrDefault();
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            using var con = new SqlConnection(_connectionString);

            var ordersSql = @"
                SELECT o.*, 
                       oi.Id, oi.FoodItemId, oi.FoodItemName, oi.Price, oi.Quantity, oi.ImageUrl
                FROM Orders o
                LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
                WHERE o.UserId = @UserId 
                ORDER BY o.OrderDate DESC";

            var orderDictionary = new Dictionary<int, Order>();

            var result = await con.QueryAsync<Order, OrderItem, Order>(
                ordersSql,
                (order, orderItem) =>
                {
                    if (!orderDictionary.TryGetValue(order.Id, out var orderEntry))
                    {
                        orderEntry = order;
                        orderEntry.OrderItems = new List<OrderItem>();
                        orderDictionary.Add(orderEntry.Id, orderEntry);
                    }

                    if (orderItem != null)
                    {
                        orderEntry.OrderItems.Add(orderItem);
                    }

                    return orderEntry;
                },
                new { UserId = userId },
                splitOn: "Id"
            );

            return orderDictionary.Values.ToList();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                _logger.LogInformation($"Attempting to update order {orderId} status to '{status}'");

                using var connection = new SqlConnection(_connectionString);

                // Simple update query
                const string sql = "UPDATE Orders SET Status = @Status WHERE Id = @OrderId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    OrderId = orderId,
                    Status = status
                });

                _logger.LogInformation($"Update query executed. Rows affected: {rowsAffected}");

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Successfully updated order {orderId} status to '{status}'");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"No rows affected. Order {orderId} may not exist.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order status for ID: {orderId}");
                return false;
            }
        }

        public async Task<string> GenerateTrackingNumberAsync()
        {
            using var con = new SqlConnection(_connectionString);

            // Generate a unique tracking number
            string trackingNumber;
            bool isUnique;

            do
            {
                trackingNumber = "TRK" + DateTime.UtcNow.ToString("yyMMddHHmmss") +
                               new Random().Next(1000, 9999).ToString();

                var sql = "SELECT COUNT(*) FROM Orders WHERE TrackingNumber = @TrackingNumber";
                var count = await con.ExecuteScalarAsync<int>(sql, new { TrackingNumber = trackingNumber });
                isUnique = count == 0;
            } while (!isUnique);

            return trackingNumber;
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            using var con = new SqlConnection(_connectionString);

            // Generate a unique order number
            string orderNumber;
            bool isUnique;

            do
            {
                orderNumber = "ORD" + DateTime.UtcNow.ToString("yyMMddHHmm") +
                            new Random().Next(100, 999).ToString();

                var sql = "SELECT COUNT(*) FROM Orders WHERE OrderNumber = @OrderNumber";
                var count = await con.ExecuteScalarAsync<int>(sql, new { OrderNumber = orderNumber });
                isUnique = count == 0;
            } while (!isUnique);

            return orderNumber;
        }

        public async Task<Order> GetOrderWithItemsAsync(int orderId)
        {
            using var connection = new SqlConnection(_connectionString);

            const string sql = @"
                SELECT o.*, 
                       oi.Id, oi.FoodItemId, oi.FoodItemName, oi.Price, oi.Quantity, oi.ImageUrl
                FROM Orders o
                LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
                WHERE o.Id = @OrderId";

            var orderDictionary = new Dictionary<int, Order>();

            var result = await connection.QueryAsync<Order, OrderItem, Order>(
                sql,
                (order, orderItem) =>
                {
                    if (!orderDictionary.TryGetValue(order.Id, out var orderEntry))
                    {
                        orderEntry = order;
                        orderEntry.OrderItems = new List<OrderItem>();
                        orderDictionary.Add(orderEntry.Id, orderEntry);
                    }

                    if (orderItem != null)
                    {
                        orderEntry.OrderItems.Add(orderItem);
                    }

                    return orderEntry;
                },
                new { OrderId = orderId },
                splitOn: "Id"
            );

            return orderDictionary.Values.FirstOrDefault();
        }

        // Add this method
        public Order GetOrderById(int orderId)
        {
            using var connection = new SqlConnection(_connectionString);

            var orderSql = @"
                SELECT o.*, 
                       oi.Id, oi.FoodItemId, oi.FoodItemName, oi.Price, oi.Quantity, oi.ImageUrl
                FROM Orders o
                LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
                WHERE o.Id = @OrderId";

            var orderDictionary = new Dictionary<int, Order>();

            var result = connection.Query<Order, OrderItem, Order>(
                orderSql,
                (order, orderItem) =>
                {
                    if (!orderDictionary.TryGetValue(order.Id, out var orderEntry))
                    {
                        orderEntry = order;
                        orderEntry.OrderItems = new List<OrderItem>();
                        orderDictionary.Add(orderEntry.Id, orderEntry);
                    }

                    if (orderItem != null)
                    {
                        orderEntry.OrderItems.Add(orderItem);
                    }

                    return orderEntry;
                },
                new { OrderId = orderId },
                splitOn: "Id"
            );

            return result.FirstOrDefault();
        }
    }
}