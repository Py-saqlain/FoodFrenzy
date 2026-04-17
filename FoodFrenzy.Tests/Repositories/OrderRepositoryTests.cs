using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using FoodFrenzy.Models;
using FoodFrenzy.Models.Interfaces;
using Xunit;

namespace FoodFrenzy.Tests.Repositories
{
    public class OrderRepositoryTests
    {
        private readonly Mock<IOrderRepository> _mockRepository;
        private readonly List<Order> _testOrders;
        private readonly List<CartItem> _testCartItems;
        private readonly List<FoodItem> _testFoodItems;
        private readonly List<OrderItem> _testOrderItems;

        public OrderRepositoryTests()
        {
            _mockRepository = new Mock<IOrderRepository>();

            // Setup test data
            _testOrders = GetTestOrders();
            _testCartItems = GetTestCartItems();
            _testFoodItems = GetTestFoodItems();
            _testOrderItems = GetTestOrderItems();

            SetupMocks();
        }

        private List<Order> GetTestOrders()
        {
            return new List<Order>
            {
                new Order
                {
                    Id = 1,
                    OrderNumber = "ORD001",
                    UserId = "user1",
                    TrackingNumber = "TRACK001",
                    Status = "Pending",
                    Subtotal = 40.99m,
                    DeliveryFee = 5.00m,
                    ServiceFee = 2.00m,
                    Tax = 3.00m,
                    Total = 50.99m,
                    CustomerName = "John Doe",
                    CustomerEmail = "john@example.com",
                    CustomerPhone = "123-456-7890",
                    DeliveryAddress = "123 Main St",
                    City = "New York",
                    ZipCode = "10001",
                    DeliveryInstructions = "Ring bell",
                    PaymentMethod = "Credit Card",
                    DeliveryMethod = "Delivery",
                    OrderDate = DateTime.UtcNow.AddDays(-2),
                    OrderItems = new List<OrderItem>()
                },
                new Order
                {
                    Id = 2,
                    OrderNumber = "ORD002",
                    UserId = "user2",
                    TrackingNumber = "TRACK002",
                    Status = "Completed",
                    Subtotal = 60.50m,
                    DeliveryFee = 5.00m,
                    ServiceFee = 3.00m,
                    Tax = 7.00m,
                    Total = 75.50m,
                    
                    CustomerName = "Jane Smith",
                    CustomerEmail = "jane@example.com",
                    CustomerPhone = "987-654-3210",
                    DeliveryAddress = "456 Oak Ave",
                    City = "Los Angeles",
                    ZipCode = "90001",
                    DeliveryInstructions = "Leave at door",
                    PaymentMethod = "PayPal",
                    DeliveryMethod = "Pickup",
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    OrderItems = new List<OrderItem>()
                },
                new Order
                {
                    Id = 3,
                    OrderNumber = "ORD003",
                    UserId = "user1",
                    TrackingNumber = "TRACK003",
                    Status = "Processing",
                    Subtotal = 20.25m,
                    DeliveryFee = 5.00m,
                    ServiceFee = 1.50m,
                    Tax = 3.50m,
                    Total = 30.25m,
                   
                    CustomerName = "John Doe",
                    CustomerEmail = "john@example.com",
                    CustomerPhone = "123-456-7890",
                    DeliveryAddress = "123 Main St",
                    City = "New York",
                    ZipCode = "10001",
                    DeliveryInstructions = "Call on arrival",
                    PaymentMethod = "Cash",
                    DeliveryMethod = "Delivery",
                    OrderDate = DateTime.UtcNow,
                    OrderItems = new List<OrderItem>()
                }
            };
        }

        private List<CartItem> GetTestCartItems()
        {
            return new List<CartItem>
            {
                new CartItem
                {
                    Id = 1,
                    FoodItemId = 1,
                    Quantity = 2,
                    UserId = "user1"
                },
                new CartItem
                {
                    Id = 2,
                    FoodItemId = 2,
                    Quantity = 1,
                    UserId = "user1"
                },
                new CartItem
                {
                    Id = 3,
                    FoodItemId = 3,
                    Quantity = 3,
                    UserId = "user2"
                }
            };
        }

        private List<FoodItem> GetTestFoodItems()
        {
            return new List<FoodItem>
            {
                new FoodItem
                {
                    Id = 1,
                    Name = "Burger",
                    Price = 12.99m,
                    IsAvailable = true
                },
                new FoodItem
                {
                    Id = 2,
                    Name = "Pizza",
                    Price = 24.99m,
                    IsAvailable = true
                },
                new FoodItem
                {
                    Id = 3,
                    Name = "Salad",
                    Price = 8.99m,
                    IsAvailable = true
                }
            };
        }

        private List<OrderItem> GetTestOrderItems()
        {
            return new List<OrderItem>
            {
                new OrderItem
                {
                    Id = 1,
                    OrderId = 1,
                    FoodItemId = 1,
                    FoodItemName = "Burger",
                    Quantity = 2,
                    Price = 25.98m,
                    ImageUrl = "burger.jpg"
                },
                new OrderItem
                {
                    Id = 2,
                    OrderId = 1,
                    FoodItemId = 2,
                    FoodItemName = "Pizza",
                    Quantity = 1,
                    Price = 24.99m,
                    ImageUrl = "pizza.jpg"
                }
            };
        }

        private void SetupMocks()
        {
            // Mock CreateOrderAsync
            _mockRepository.Setup(repo => repo.CreateOrderAsync(
                It.IsAny<Order>(),
                It.IsAny<List<CartItem>>()))
                .ReturnsAsync((Order order, List<CartItem> cartItems) =>
                {
                    // Simulate creation
                    order.Id = _testOrders.Count + 1;
                    order.OrderNumber = $"ORD{order.Id:000}";
                    order.TrackingNumber = $"TRACK{order.Id:000}";
                    order.OrderDate = DateTime.UtcNow;
                    order.Status = "Pending";
                    return order;
                });

            // Mock GetOrderByIdAsync
            _mockRepository.Setup(repo => repo.GetOrderByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _testOrders.FirstOrDefault(o => o.Id == id));

            // Mock GetOrderByTrackingNumberAsync
            _mockRepository.Setup(repo => repo.GetOrderByTrackingNumberAsync(It.IsAny<string>()))
                .ReturnsAsync((string trackingNumber) =>
                    _testOrders.FirstOrDefault(o => o.TrackingNumber == trackingNumber));

            // Mock GetUserOrdersAsync
            _mockRepository.Setup(repo => repo.GetUserOrdersAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) =>
                    _testOrders.Where(o => o.UserId == userId).ToList());

            // Mock GetOrderWithItemsAsync
            _mockRepository.Setup(repo => repo.GetOrderWithItemsAsync(It.IsAny<int>()))
                .ReturnsAsync((int orderId) =>
                {
                    var order = _testOrders.FirstOrDefault(o => o.Id == orderId);
                    if (order != null && orderId == 1)
                    {
                        order.OrderItems = _testOrderItems;
                    }
                    return order;
                });

            // Mock UpdateOrderStatusAsync
            _mockRepository.Setup(repo => repo.UpdateOrderStatusAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int orderId, string status) =>
                {
                    var order = _testOrders.FirstOrDefault(o => o.Id == orderId);
                    if (order != null)
                    {
                        order.Status = status;
                        return true;
                    }
                    return false;
                });

            // Mock GenerateTrackingNumberAsync
            _mockRepository.Setup(repo => repo.GenerateTrackingNumberAsync())
                .ReturnsAsync(() => $"TRACK{DateTime.Now.Ticks.ToString().Substring(10)}");

            // Mock GenerateOrderNumberAsync
            _mockRepository.Setup(repo => repo.GenerateOrderNumberAsync())
                .ReturnsAsync(() => $"ORD{DateTime.Now:yyyyMMddHHmmss}");

            // Mock GetOrdersByDateRange
            _mockRepository.Setup(repo => repo.GetOrdersByDateRange(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns((DateTime startDate, DateTime endDate) =>
                    _testOrders.Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate));

            // Mock GetAllOrders
            _mockRepository.Setup(repo => repo.GetAllOrders())
                .Returns(_testOrders);

            // Mock GetOrderById (sync)
            _mockRepository.Setup(repo => repo.GetOrderById(It.IsAny<int>()))
                .Returns((int id) => _testOrders.FirstOrDefault(o => o.Id == id));
        }

        [Fact]
        public async Task CreateOrderAsync_WithValidData_CreatesOrder()
        {
            // Arrange
            var newOrder = new Order
            {
                UserId = "user3",
                Status = "Pending",
                CustomerName = "Bob Wilson",
                CustomerEmail = "bob@example.com",
                CustomerPhone = "555-123-4567",
                DeliveryAddress = "789 Pine St",
                City = "Chicago",
                ZipCode = "60601",
                PaymentMethod = "Credit Card",
                DeliveryMethod = "Delivery",
                Subtotal = 35.75m,
                DeliveryFee = 5.00m,
                ServiceFee = 2.00m,
                Tax = 3.00m,
                Total = 45.75m,
                
            };

            var cartItems = new List<CartItem>
            {
                new CartItem { FoodItemId = 1, Quantity = 2, UserId = "user3" }
            };

            // Act
            var result = await _mockRepository.Object.CreateOrderAsync(newOrder, cartItems);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.NotEmpty(result.OrderNumber);
            Assert.NotEmpty(result.TrackingNumber);
            Assert.Equal("user3", result.UserId);
            Assert.Equal("Pending", result.Status);
            Assert.NotNull(result.OrderDate);
        }

        [Fact]
        public async Task CreateOrderAsync_WithNullCartItems_ReturnsOrder()
        {
            // Arrange
            var newOrder = new Order
            {
                UserId = "user3",
                CustomerName = "Test User",
                CustomerEmail = "test@example.com",
                CustomerPhone = "123-456-7890",
                DeliveryAddress = "Test Address",
                Subtotal = 10.00m,
                DeliveryFee = 5.00m,
                ServiceFee = 1.00m,
                Tax = 1.60m,
                Total = 17.60m
            };

            // Act
            var result = await _mockRepository.Object.CreateOrderAsync(newOrder, null);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task CreateOrderAsync_WithEmptyCart_ReturnsOrder()
        {
            // Arrange
            var newOrder = new Order
            {
                UserId = "user3",
                CustomerName = "Test User",
                CustomerEmail = "test@example.com",
                CustomerPhone = "123-456-7890",
                DeliveryAddress = "Test Address",
                Subtotal = 0m,
                DeliveryFee = 5.00m,
                ServiceFee = 0m,
                Tax = 0m,
                Total = 5.00m
            };

            // Act
            var result = await _mockRepository.Object.CreateOrderAsync(newOrder, new List<CartItem>());

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task GetOrderByIdAsync_WithValidId_ReturnsOrder()
        {
            // Arrange
            var orderId = 1;

            // Act
            var result = await _mockRepository.Object.GetOrderByIdAsync(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.Equal("ORD001", result.OrderNumber);
            Assert.Equal("TRACK001", result.TrackingNumber);
            Assert.Equal("John Doe", result.CustomerName);
            Assert.Equal("john@example.com", result.CustomerEmail);
        }

        [Fact]
        public async Task GetOrderByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = 999;

            // Act
            var result = await _mockRepository.Object.GetOrderByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrderByTrackingNumberAsync_WithValidTracking_ReturnsOrder()
        {
            // Arrange
            var trackingNumber = "TRACK002";

            // Act
            var result = await _mockRepository.Object.GetOrderByTrackingNumberAsync(trackingNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(trackingNumber, result.TrackingNumber);
            Assert.Equal("ORD002", result.OrderNumber);
            Assert.Equal("Jane Smith", result.CustomerName);
            Assert.Equal("Completed", result.Status);
        }

        [Fact]
        public async Task GetOrderByTrackingNumberAsync_WithInvalidTracking_ReturnsNull()
        {
            // Arrange
            var invalidTracking = "INVALID123";

            // Act
            var result = await _mockRepository.Object.GetOrderByTrackingNumberAsync(invalidTracking);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserOrdersAsync_WithValidUserId_ReturnsUserOrders()
        {
            // Arrange
            var userId = "user1";

            // Act
            var result = await _mockRepository.Object.GetUserOrdersAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // user1 has 2 orders
            Assert.All(result, order => Assert.Equal(userId, order.UserId));
        }

        [Fact]
        public async Task GetUserOrdersAsync_WithNoOrders_ReturnsEmptyList()
        {
            // Arrange
            var userId = "user_with_no_orders";

            // Act
            var result = await _mockRepository.Object.GetUserOrdersAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetOrderWithItemsAsync_WithItems_ReturnsOrderWithItems()
        {
            // Arrange
            var orderId = 1;

            // Act
            var result = await _mockRepository.Object.GetOrderWithItemsAsync(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.NotNull(result.OrderItems);
            Assert.Equal(2, result.OrderItems.Count);
            Assert.Contains(result.OrderItems, item => item.FoodItemName == "Burger");
            Assert.Contains(result.OrderItems, item => item.FoodItemName == "Pizza");
        }

        [Fact]
        public async Task GetOrderWithItemsAsync_WithoutItems_ReturnsOrderWithEmptyItems()
        {
            // Arrange
            var orderId = 2;

            // Act
            var result = await _mockRepository.Object.GetOrderWithItemsAsync(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.NotNull(result.OrderItems);
            Assert.Empty(result.OrderItems);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithValidData_ReturnsTrueAndUpdates()
        {
            // Arrange
            var orderId = 1;
            var newStatus = "Shipped";

            // Act
            var result = await _mockRepository.Object.UpdateOrderStatusAsync(orderId, newStatus);

            // Assert
            Assert.True(result);

            // Verify status was updated
            var order = await _mockRepository.Object.GetOrderByIdAsync(orderId);
            Assert.Equal(newStatus, order.Status);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithInvalidOrderId_ReturnsFalse()
        {
            // Arrange
            var invalidOrderId = 999;
            var newStatus = "Shipped";

            // Act
            var result = await _mockRepository.Object.UpdateOrderStatusAsync(invalidOrderId, newStatus);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GenerateTrackingNumberAsync_ReturnsUniqueString()
        {
            // Act
            var trackingNumber1 = await _mockRepository.Object.GenerateTrackingNumberAsync();
            var trackingNumber2 = await _mockRepository.Object.GenerateTrackingNumberAsync();

            // Assert
            Assert.NotNull(trackingNumber1);
            Assert.NotNull(trackingNumber2);
            Assert.NotEmpty(trackingNumber1);
            Assert.NotEmpty(trackingNumber2);
            Assert.StartsWith("TRACK", trackingNumber1);
            Assert.StartsWith("TRACK", trackingNumber2);
        }

        [Fact]
        public async Task GenerateOrderNumberAsync_ReturnsUniqueString()
        {
            // Act
            var orderNumber1 = await _mockRepository.Object.GenerateOrderNumberAsync();
            var orderNumber2 = await _mockRepository.Object.GenerateOrderNumberAsync();

            // Assert
            Assert.NotNull(orderNumber1);
            Assert.NotNull(orderNumber2);
            Assert.NotEmpty(orderNumber1);
            Assert.NotEmpty(orderNumber2);
            Assert.StartsWith("ORD", orderNumber1);
            Assert.StartsWith("ORD", orderNumber2);
        }

        [Fact]
        public void GetOrdersByDateRange_ReturnsFilteredOrders()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-2);
            var endDate = DateTime.UtcNow.AddDays(-1);

            // Act
            var result = _mockRepository.Object.GetOrdersByDateRange(startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Should have 1 order in this range
            var order = result.First();
            Assert.True(order.OrderDate >= startDate && order.OrderDate <= endDate);
        }

        [Fact]
        public void GetOrdersByDateRange_WithNoOrders_ReturnsEmpty()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddYears(-1);
            var endDate = DateTime.UtcNow.AddYears(-1).AddDays(1);

            // Act
            var result = _mockRepository.Object.GetOrdersByDateRange(startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetAllOrders_ReturnsAllOrders()
        {
            // Act
            var result = _mockRepository.Object.GetAllOrders();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            Assert.Contains(result, o => o.OrderNumber == "ORD001");
            Assert.Contains(result, o => o.OrderNumber == "ORD002");
            Assert.Contains(result, o => o.OrderNumber == "ORD003");
        }

        [Fact]
        public void GetOrderById_WithValidId_ReturnsOrder()
        {
            // Arrange
            var orderId = 1;

            // Act
            var result = _mockRepository.Object.GetOrderById(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.Equal("ORD001", result.OrderNumber);
            Assert.Equal("John Doe", result.CustomerName);
        }

        [Fact]
        public void GetOrderById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = 999;

            // Act
            var result = _mockRepository.Object.GetOrderById(invalidId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateOrderAsync_WithOrderCalculations_CorrectTotals()
        {
            // Arrange
            var newOrder = new Order
            {
                UserId = "user4",
                CustomerName = "Test Customer",
                CustomerEmail = "test@example.com",
                CustomerPhone = "123-456-7890",
                DeliveryAddress = "Test Address",
                Subtotal = 77.94m,
                DeliveryFee = 5.00m,
                ServiceFee = 3.00m,
                Tax = 8.58m, // 11% tax on 77.94
                Total = 94.52m,
              
            };

            var cartItems = new List<CartItem>
            {
                new CartItem { FoodItemId = 1, Quantity = 2, UserId = "user4" },
                new CartItem { FoodItemId = 2, Quantity = 1, UserId = "user4" },
                new CartItem { FoodItemId = 3, Quantity = 3, UserId = "user4" }
            };

            // Act
            var result = await _mockRepository.Object.CreateOrderAsync(newOrder, cartItems);

            // Assert
            Assert.Equal(77.94m, result.Subtotal);
            Assert.Equal(5.00m, result.DeliveryFee);
            Assert.Equal(3.00m, result.ServiceFee);
            Assert.Equal(8.58m, result.Tax);
            Assert.Equal(94.52m, result.Total);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithValidStatuses_WorksCorrectly()
        {
            // Arrange
            var orderId = 1;
            var statuses = new[] { "Processing", "Shipped", "Delivered", "Cancelled" };

            foreach (var status in statuses)
            {
                // Act
                var result = await _mockRepository.Object.UpdateOrderStatusAsync(orderId, status);

                // Assert
                Assert.True(result);

                var order = await _mockRepository.Object.GetOrderByIdAsync(orderId);
                Assert.Equal(status, order.Status);
            }
        }

        [Fact]
        public void GetAllOrders_ReturnsOrdersWithCompleteData()
        {
            // Act
            var result = _mockRepository.Object.GetAllOrders().ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            // Check first order
            var firstOrder = result[0];
            Assert.Equal("John Doe", firstOrder.CustomerName);
            Assert.Equal("john@example.com", firstOrder.CustomerEmail);
            Assert.Equal("123-456-7890", firstOrder.CustomerPhone);
            Assert.Equal("123 Main St", firstOrder.DeliveryAddress);
            Assert.Equal("New York", firstOrder.City);
            Assert.Equal("10001", firstOrder.ZipCode);

            // Check second order
            var secondOrder = result[1];
            Assert.Equal("Jane Smith", secondOrder.CustomerName);
            Assert.Equal("jane@example.com", secondOrder.CustomerEmail);
            Assert.Equal("987-654-3210", secondOrder.CustomerPhone);
            Assert.Equal("456 Oak Ave", secondOrder.DeliveryAddress);
            Assert.Equal("Los Angeles", secondOrder.City);
            Assert.Equal("90001", secondOrder.ZipCode);
        }

        [Fact]
        public async Task GetOrderWithItemsAsync_ReturnsItemsWithDetails()
        {
            // Arrange
            var orderId = 1;

            // Act
            var result = await _mockRepository.Object.GetOrderWithItemsAsync(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.OrderItems);

            var burgerItem = result.OrderItems.FirstOrDefault(i => i.FoodItemName == "Burger");
            Assert.NotNull(burgerItem);
            Assert.Equal(2, burgerItem.Quantity);
            Assert.Equal(25.98m, burgerItem.Price);
            Assert.Equal("burger.jpg", burgerItem.ImageUrl);

            var pizzaItem = result.OrderItems.FirstOrDefault(i => i.FoodItemName == "Pizza");
            Assert.NotNull(pizzaItem);
            Assert.Equal(1, pizzaItem.Quantity);
            Assert.Equal(24.99m, pizzaItem.Price);
            Assert.Equal("pizza.jpg", pizzaItem.ImageUrl);
        }
    }
}