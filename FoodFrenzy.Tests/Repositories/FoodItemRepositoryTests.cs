using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Moq;
using FoodFrenzy.Models;
using FoodFrenzy.Models.Repositories;
using FoodFrenzy.Tests.Mocks;
using Xunit;

namespace FoodFrenzy.Tests.Repositories
{
    public class FoodItemRepositoryTests : IDisposable
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IConfigurationSection> _mockConnectionStringSection;
        private readonly SqlConnection _testConnection;
        private readonly string _testConnectionString;
        private readonly FoodItemRepository _repository;

        public FoodItemRepositoryTests()
        {
            // Create in-memory database or use test database
            _testConnectionString = "Server=(localdb)\\mssqllocaldb;Database=FoodFrenzyTest;Trusted_Connection=True;MultipleActiveResultSets=true";

            _mockConnectionStringSection = new Mock<IConfigurationSection>();
            _mockConnectionStringSection.Setup(x => x.Value).Returns(_testConnectionString);

            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(x => x.GetSection("ConnectionStrings")["DefaultConnection"])
                             .Returns(_testConnectionString);

            _repository = new FoodItemRepository(_mockConfiguration.Object);

            // Initialize test database
            InitializeTestDatabase();
        }

        private void InitializeTestDatabase()
        {
            using var connection = new SqlConnection(_testConnectionString);
            connection.Open();

            // Create FoodItems table
            var createTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FoodItems' and xtype='U')
                BEGIN
                    CREATE TABLE FoodItems (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Name NVARCHAR(100) NOT NULL,
                        Category NVARCHAR(50),
                        Price DECIMAL(10,2) NOT NULL,
                        Rating FLOAT NOT NULL DEFAULT 0.0,
                        IsAvailable BIT NOT NULL DEFAULT 1,
                        Description NVARCHAR(500),
                        ImageUrl NVARCHAR(500)
                    )
                END";

            connection.Execute(createTableSql);

            // Clear existing data
            connection.Execute("DELETE FROM FoodItems");

            // Insert test data
            var testData = MockData.GetFoodItems();
            foreach (var item in testData)
            {
                connection.Execute(@"
                    INSERT INTO FoodItems (Name, Category, Price, Rating, IsAvailable, Description, ImageUrl)
                    VALUES (@Name, @Category, @Price, @Rating, @IsAvailable, @Description, @ImageUrl)",
                    item);
            }
        }

        [Fact]
        public void GetAllFoodItems_ReturnsAllItems()
        {
            // Act
            var result = _repository.GetAllFoodItems().ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Burger", result[0].Name);
            Assert.Equal("Pizza", result[1].Name);
            Assert.Equal("Salad", result[2].Name);
        }

        [Fact]
        public void GetTopThreeFoodItems_ReturnsOnlyAvailableItems()
        {
            // Act
            var result = _repository.GetTopThreeFoodItems().ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Only 2 items are available
            Assert.All(result, item => Assert.True(item.IsAvailable));
            Assert.Equal("Pizza", result[0].Name); // Highest rating first
            Assert.Equal("Burger", result[1].Name);
        }

        [Fact]
        public void GetFoodItemById_WithValidId_ReturnsItem()
        {
            // Act
            var result = _repository.GetFoodItemById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Burger", result.Name);
            Assert.Equal(9.99m, result.Price);
        }

        [Fact]
        public void GetFoodItemById_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = _repository.GetFoodItemById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void AddFoodItem_WithValidItem_ReturnsTrue()
        {
            // Arrange
            var newItem = new FoodItem
            {
                Name = "Pasta",
                Category = "Italian",
                Price = 11.99m,
                Rating = 4.3,
                IsAvailable = true,
                Description = "Creamy pasta",
                ImageUrl = "pasta.jpg"
            };

            // Act
            var result = _repository.AddFoodItem(newItem);

            // Assert
            Assert.True(result);

            // Verify item was added
            var addedItem = _repository.GetFoodItemById(4); // Next ID
            Assert.NotNull(addedItem);
            Assert.Equal("Pasta", addedItem.Name);
        }

        [Fact]
        public void AddFoodItem_WithNullName_HandlesCorrectly()
        {
            // Arrange
            var newItem = new FoodItem
            {
                Name = null,
                Category = "Test",
                Price = 5.99m,
                Rating = 3.5,
                IsAvailable = true
            };

            // Act
            var result = _repository.AddFoodItem(newItem);

            // Assert
            Assert.True(result); // Should handle null with empty string
        }

        [Fact]
        public void UpdateFoodItem_WithValidItem_ReturnsTrue()
        {
            // Arrange
            var updatedItem = new FoodItem
            {
                Id = 1,
                Name = "Updated Burger",
                Category = "Fast Food",
                Price = 10.99m,
                Rating = 4.6,
                IsAvailable = true,
                Description = "Updated description",
                ImageUrl = "updated.jpg"
            };

            // Act
            var result = _repository.UpdateFoodItem(updatedItem);

            // Assert
            Assert.True(result);

            // Verify update
            var item = _repository.GetFoodItemById(1);
            Assert.Equal("Updated Burger", item.Name);
            Assert.Equal(10.99m, item.Price);
        }

        [Fact]
        public void UpdateFoodItem_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var updatedItem = new FoodItem
            {
                Id = 999,
                Name = "Non-existent"
            };

            // Act
            var result = _repository.UpdateFoodItem(updatedItem);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DeleteFoodItem_WithValidId_ReturnsTrue()
        {
            // Act
            var result = _repository.DeleteFoodItem(1);

            // Assert
            Assert.True(result);

            // Verify deletion
            var item = _repository.GetFoodItemById(1);
            Assert.Null(item);
        }

        [Fact]
        public void DeleteFoodItem_WithInvalidId_ReturnsFalse()
        {
            // Act
            var result = _repository.DeleteFoodItem(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FoodItemHasOrders_ReturnsCorrectValue()
        {
            // Arrange - Create related tables for this test
            using var connection = new SqlConnection(_testConnectionString);
            connection.Execute(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' and xtype='U')
                BEGIN
                    CREATE TABLE OrderItems (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        FoodItemId INT NOT NULL,
                        OrderId INT NOT NULL,
                        Quantity INT NOT NULL
                    )
                END");

            // Insert test order item
            connection.Execute(
                "INSERT INTO OrderItems (FoodItemId, OrderId, Quantity) VALUES (1, 1, 2)");

            // Act
            var result = _repository.FoodItemHasOrders(1);

            // Assert
            Assert.True(result);
        }

        public void Dispose()
        {
            // Cleanup test database
            using var connection = new SqlConnection(_testConnectionString);
            connection.Execute("DELETE FROM OrderItems");
            connection.Execute("DELETE FROM FoodItems");
        }
    }
}