using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FoodFrenzy.Models;
using FoodFrenzy.Models.Repositories;
using FoodFrenzy.IntegrationTests.Database;
using Xunit;

namespace FoodFrenzy.IntegrationTests.Repositories
{
    [Collection("Database collection")]
    public class OrderRepositoryIntegrationTests : IAsyncLifetime
    {
        private readonly OrderRepository _repository;
        private readonly TestDatabaseFixture _databaseFixture;
        private readonly Mock<ILogger<OrderRepository>> _mockLogger;

        public OrderRepositoryIntegrationTests(TestDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
            _mockLogger = new Mock<ILogger<OrderRepository>>();

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(x => x.GetConnectionString("DefaultConnection"))
                     .Returns(_databaseFixture.ConnectionString);

            _repository = new OrderRepository(mockConfig.Object, _mockLogger.Object);
        }

        public async Task InitializeAsync()
        {
            await TestDatabaseSeeder.SeedTestDataAsync(_databaseFixture.ConnectionString);
        }

        public Task DisposeAsync()
        {
            // Cleanup handled by fixture
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetAllOrders_ReturnsSeededOrders()
        {
            // Act
            var orders = _repository.GetAllOrders().ToList();

            // Assert
            Assert.NotNull(orders);
            Assert.Equal(2, orders.Count);
            
            var firstOrder = orders.First(o => o.OrderNumber == "TEST001");
            Assert.Equal("test_user_1", firstOrder.UserId);
            Assert.Equal("Completed", firstOrder.Status);
            Assert.Equal(49.48m, firstOrder.Total);
            Assert.Equal(2, firstOrder.OrderItems.Count);
        }

        [Fact]
        public async Task GetOrderByIdAsync_WithValidId_ReturnsOrder()
        {
            // Arrange
            var orderId = 1; // First seeded order

            // Act
            var order = await _repository.GetOrderByIdAsync(orderId);

            // Assert
            Assert.NotNull(order);
            Assert.Equal("TEST001", order.OrderNumber);
            Assert.Equal("test_user_1", order.UserId);
            Assert.Equal("John Test", order.CustomerName);
            Assert.Equal("john.test@example.com", order.CustomerEmail);
            Assert.Equal(2, order.OrderItems.Count);
        }

        [Fact]
        public async Task GetOrderByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var order = await _repository.GetOrderByIdAsync(999);

            // Assert
            Assert.Null(order);
        }

        [Fact]
        public async Task CreateOrderAsync_WithValidData_CreatesOrderSuccessfully()
        {
            // Arrange
            var newOrder = new Order
            {
                UserId = "new_test_user",
                Subtotal = 25.98m,
                DeliveryFee = 4.00m,
                ServiceFee = 2.00m,
                Tax = 3.20m,
                Total = 35.18m,
                CustomerName = "New Customer",
                CustomerEmail = "new@example.com",
                CustomerPhone = "555-123-4567",
                DeliveryAddress = "789 New St",
                City = "New City",
                ZipCode = "67890",
                DeliveryInstructions = "Call on arrival",
                PaymentMethod = "Cash",
                DeliveryMethod = "Delivery",
                Status = "Pending"
            };

            var cartItems = new List<CartItem>
            {
                new CartItem { FoodItemId = 1, Quantity = 2, UserId = "new_test_user", Name = "Test Burger", Price = 12.99m, ImageUrl = "burger.jpg" }
            };

            // Act
            var createdOrder = await _repository.CreateOrderAsync(newOrder, cartItems);

            // Assert
            Assert.NotNull(createdOrder);
            Assert.True(createdOrder.Id > 0);
            Assert.NotEmpty(createdOrder.OrderNumber);
            Assert.NotEmpty(createdOrder.TrackingNumber);
            Assert.Equal("new_test_user", createdOrder.UserId);
            Assert.Equal("New Customer", createdOrder.CustomerName);
            Assert.Equal("Pending", createdOrder.Status);
            
            // Verify order was saved to database
            var retrievedOrder = await _repository.GetOrderByIdAsync(createdOrder.Id);
            Assert.NotNull(retrievedOrder);
            Assert.Equal(createdOrder.OrderNumber, retrievedOrder.OrderNumber);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ValidOrder_UpdatesSuccessfully()
        {
            // Arrange
            var orderId = 2; // Second seeded order
            var newStatus = "Shipped";

            // Act
            var result = await _repository.UpdateOrderStatusAsync(orderId, newStatus);

            // Assert
            Assert.True(result);
            
            // Verify update
            var updatedOrder = await _repository.GetOrderByIdAsync(orderId);
            Assert.Equal(newStatus, updatedOrder.Status);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_InvalidOrder_ReturnsFalse()
        {
            // Act
            var result = await _repository.UpdateOrderStatusAsync(999, "Shipped");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetUserOrdersAsync_ReturnsUserOrders()
        {
            // Arrange
            var userId = "test_user_1";

            // Act
            var orders = await _repository.GetUserOrdersAsync(userId);

            // Assert
            Assert.NotNull(orders);
            Assert.Single(orders);
            Assert.All(orders, o => Assert.Equal(userId, o.UserId));
        }

        [Fact]
        public async Task GetUserOrdersAsync_NoOrders_ReturnsEmptyList()
        {
            // Act
            var orders = await _repository.GetUserOrdersAsync("non_existent_user");

            // Assert
            Assert.NotNull(orders);
            Assert.Empty(orders);
        }

        [Fact]
        public async Task GetOrderByTrackingNumberAsync_ReturnsCorrectOrder()
        {
            // Act
            var order = await _repository.GetOrderByTrackingNumberAsync("TRK_TEST001");

            // Assert
            Assert.NotNull(order);
            Assert.Equal("TEST001", order.OrderNumber);
            Assert.Equal("John Test", order.CustomerName);
        }

        [Fact]
        public async Task GenerateOrderNumberAsync_ReturnsUniqueNumber()
        {
            // Act
            var orderNumber1 = await _repository.GenerateOrderNumberAsync();
            var orderNumber2 = await _repository.GenerateOrderNumberAsync();

            // Assert
            Assert.NotEmpty(orderNumber1);
            Assert.NotEmpty(orderNumber2);
            Assert.NotEqual(orderNumber1, orderNumber2);
            Assert.StartsWith("ORD", orderNumber1);
            Assert.StartsWith("ORD", orderNumber2);
        }

        [Fact]
        public async Task GenerateTrackingNumberAsync_ReturnsUniqueNumber()
        {
            // Act
            var trackingNumber1 = await _repository.GenerateTrackingNumberAsync();
            var trackingNumber2 = await _repository.GenerateTrackingNumberAsync();

            // Assert
            Assert.NotEmpty(trackingNumber1);
            Assert.NotEmpty(trackingNumber2);
            Assert.NotEqual(trackingNumber1, trackingNumber2);
            Assert.StartsWith("TRK", trackingNumber1);
            Assert.StartsWith("TRK", trackingNumber2);
        }

        [Fact]
        public void GetOrdersByDateRange_ReturnsFilteredOrders()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-3);
            var endDate = DateTime.UtcNow;

            // Act
            var orders = _repository.GetOrdersByDateRange(startDate, endDate).ToList();

            // Assert
            Assert.NotNull(orders);
            Assert.Equal(2, orders.Count);
            Assert.All(orders, o => 
            {
                Assert.True(o.OrderDate >= startDate && o.OrderDate <= endDate);
            });
        }

        [Fact]
        public void GetOrderById_SyncMethod_ReturnsOrder()
        {
            // Arrange
            var orderId = 1;

            // Act
            var order = _repository.GetOrderById(orderId);

            // Assert
            Assert.NotNull(order);
            Assert.Equal("TEST001", order.OrderNumber);
            Assert.Equal("test_user_1", order.UserId);
        }
    }

    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}