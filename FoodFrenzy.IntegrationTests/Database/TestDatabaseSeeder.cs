using Dapper;
using Microsoft.Data.SqlClient;
using FoodFrenzy.Models;

namespace FoodFrenzy.IntegrationTests.Database
{
    public static class TestDatabaseSeeder
    {
        public static async Task SeedTestDataAsync(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Clear existing data
            await connection.ExecuteAsync("DELETE FROM OrderItems");
            await connection.ExecuteAsync("DELETE FROM Orders");
            await connection.ExecuteAsync("DELETE FROM CartItems");
            await connection.ExecuteAsync("DELETE FROM FoodItems");

            // Seed FoodItems
            var foodItems = new[]
            {
                new { Name = "Test Burger", Category = "Fast Food", Price = 12.99m, Rating = 4.5, IsAvailable = true, Description = "Test burger", ImageUrl = "burger.jpg" },
                new { Name = "Test Pizza", Category = "Italian", Price = 24.99m, Rating = 4.7, IsAvailable = true, Description = "Test pizza", ImageUrl = "pizza.jpg" },
                new { Name = "Test Salad", Category = "Healthy", Price = 8.99m, Rating = 4.2, IsAvailable = true, Description = "Test salad", ImageUrl = "salad.jpg" },
                new { Name = "Unavailable Item", Category = "Test", Price = 5.99m, Rating = 3.0, IsAvailable = false, Description = "Not available", ImageUrl = "none.jpg" }
            };

            foreach (var item in foodItems)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO FoodItems (Name, Category, Price, Rating, IsAvailable, Description, ImageUrl)
                    VALUES (@Name, @Category, @Price, @Rating, @IsAvailable, @Description, @ImageUrl)",
                    item);
            }

            // Seed Orders
            var orders = new[]
            {
                new
                {
                    UserId = "test_user_1",
                    OrderNumber = "TEST001",
                    Subtotal = 37.98m,
                    DeliveryFee = 5.00m,
                    ServiceFee = 2.00m,
                    Tax = 4.50m,
                    Total = 49.48m,
                    CustomerName = "John Test",
                    CustomerEmail = "john.test@example.com",
                    CustomerPhone = "123-456-7890",
                    DeliveryAddress = "123 Test St",
                    City = "Test City",
                    ZipCode = "12345",
                    DeliveryInstructions = "Ring bell",
                    PaymentMethod = "Credit Card",
                    DeliveryMethod = "Delivery",
                    OrderDate = DateTime.UtcNow.AddDays(-2),
                    Status = "Completed",
                    TrackingNumber = "TRK_TEST001"
                },
                new
                {
                    UserId = "test_user_2",
                    OrderNumber = "TEST002",
                    Subtotal = 24.99m,
                    DeliveryFee = 3.00m,
                    ServiceFee = 1.50m,
                    Tax = 3.00m,
                    Total = 32.49m,
                    CustomerName = "Jane Test",
                    CustomerEmail = "jane.test@example.com",
                    CustomerPhone = "987-654-3210",
                    DeliveryAddress = "456 Test Ave",
                    City = "Test Town",
                    ZipCode = "54321",
                    DeliveryInstructions = "Leave at door",
                    PaymentMethod = "PayPal",
                    DeliveryMethod = "Pickup",
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    Status = "Processing",
                    TrackingNumber = "TRK_TEST002"
                }
            };

            foreach (var order in orders)
            {
                var orderId = await connection.ExecuteScalarAsync<int>(@"
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
                    )",
                    order);

                // Add order items for first order
                if (order.OrderNumber == "TEST001")
                {
                    var orderItems = new[]
                    {
                        new { OrderId = orderId, FoodItemId = 1, FoodItemName = "Test Burger", Price = 12.99m, Quantity = 2, ImageUrl = "burger.jpg" },
                        new { OrderId = orderId, FoodItemId = 2, FoodItemName = "Test Pizza", Price = 24.99m, Quantity = 1, ImageUrl = "pizza.jpg" }
                    };

                    foreach (var item in orderItems)
                    {
                        await connection.ExecuteAsync(@"
                            INSERT INTO OrderItems (OrderId, FoodItemId, FoodItemName, Price, Quantity, ImageUrl)
                            VALUES (@OrderId, @FoodItemId, @FoodItemName, @Price, @Quantity, @ImageUrl)",
                            item);
                    }
                }
            }

            // Seed CartItems
            var cartItems = new[]
            {
                new { FoodItemId = 1, Quantity = 2, UserId = "test_user_1" },
                new { FoodItemId = 3, Quantity = 1, UserId = "test_user_1" },
                new { FoodItemId = 2, Quantity = 1, UserId = "test_user_2" }
            };

            foreach (var item in cartItems)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO CartItems (FoodItemId, Quantity, UserId)
                    VALUES (@FoodItemId, @Quantity, @UserId)",
                    item);
            }

            Console.WriteLine("Test data seeded successfully");
        }
    }
}