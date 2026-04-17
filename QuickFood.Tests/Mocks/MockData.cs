using FoodFrenzy.Models;

namespace FoodFrenzy.Tests.Mocks
{
    public static class MockData
    {
        public static List<FoodItem> GetFoodItems()
        {
            return new List<FoodItem>
            {
                new FoodItem
                {
                    Id = 1,
                    Name = "Burger",
                    Category = "Fast Food",
                    Price = 9.99m,
                    Rating = 4.5,
                    IsAvailable = true,
                    Description = "Delicious beef burger",
                    ImageUrl = "burger.jpg"
                },
                new FoodItem
                {
                    Id = 2,
                    Name = "Pizza",
                    Category = "Italian",
                    Price = 12.99m,
                    Rating = 4.7,
                    IsAvailable = true,
                    Description = "Cheese pizza",
                    ImageUrl = "pizza.jpg"
                },
                new FoodItem
                {
                    Id = 3,
                    Name = "Salad",
                    Category = "Healthy",
                    Price = 7.99m,
                    Rating = 4.2,
                    IsAvailable = false,
                    Description = "Fresh garden salad",
                    ImageUrl = "salad.jpg"
                }
            };
        }
    }
}