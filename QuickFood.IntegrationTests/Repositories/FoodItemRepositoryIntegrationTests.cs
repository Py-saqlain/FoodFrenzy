using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using FoodFrenzy.Models;
using FoodFrenzy.Models.Repositories;
using FoodFrenzy.IntegrationTests.Database;
using Xunit;
using Moq;

namespace FoodFrenzy.IntegrationTests.Repositories
{
    [Collection("Database collection")]
    public class FoodItemRepositoryIntegrationTests : IAsyncLifetime
    {
        private readonly FoodItemRepository _repository;
        private readonly TestDatabaseFixture _databaseFixture;

        public FoodItemRepositoryIntegrationTests(TestDatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(x => x.GetConnectionString("DefaultConnection"))
                     .Returns(_databaseFixture.ConnectionString);

            _repository = new FoodItemRepository(mockConfig.Object);
        }

        public async Task InitializeAsync()
        {
            await TestDatabaseSeeder.SeedTestDataAsync(_databaseFixture.ConnectionString);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public void GetAllFoodItems_ReturnsAllItems()
        {
            // Act
            var items = _repository.GetAllFoodItems().ToList();

            // Assert
            Assert.NotNull(items);
            Assert.Equal(4, items.Count); // 4 seeded items
            Assert.Contains(items, i => i.Name == "Test Burger");
            Assert.Contains(items, i => i.Name == "Test Pizza");
            Assert.Contains(items, i => i.Name == "Test Salad");
        }

        [Fact]
        public void GetTopThreeFoodItems_ReturnsAvailableItems()
        {
            // Act
            var items = _repository.GetTopThreeFoodItems().ToList();

            // Assert
            Assert.NotNull(items);
            Assert.Equal(3, items.Count); // Only top 3 available items
            Assert.All(items, i => Assert.True(i.IsAvailable));
            Assert.Equal("Test Pizza", items[0].Name); // Highest rating first
            Assert.Equal("Test Burger", items[1].Name);
            Assert.Equal("Test Salad", items[2].Name);
        }

        [Fact]
        public void GetFoodItemById_ValidId_ReturnsItem()
        {
            // Arrange
            var foodItemId = 1;

            // Act
            var item = _repository.GetFoodItemById(foodItemId);

            // Assert
            Assert.NotNull(item);
            Assert.Equal("Test Burger", item.Name);
            Assert.Equal(12.99m, item.Price);
            Assert.True(item.IsAvailable);
        }

        [Fact]
        public void FoodItemHasOrders_WithOrders_ReturnsTrue()
        {
            // Arrange
            var foodItemId = 1; // Burger has orders

            // Act
            var hasOrders = _repository.FoodItemHasOrders(foodItemId);

            // Assert
            Assert.True(hasOrders);
        }

        [Fact]
        public void FoodItemHasOrders_WithoutOrders_ReturnsFalse()
        {
            // Arrange
            var foodItemId = 3; // Salad has no orders

            // Act
            var hasOrders = _repository.FoodItemHasOrders(foodItemId);

            // Assert
            Assert.False(hasOrders);
        }
    }
}