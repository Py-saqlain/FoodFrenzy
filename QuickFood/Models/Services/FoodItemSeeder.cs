// FoodItemSeeder.cs

// FoodItemSeeder.cs
using FoodFrenzy.Models;

namespace FoodFrenzy.Models.Services
{
    public class FoodItemSeeder
    {
        private static readonly Random _random = new Random();

        private static readonly string[] _categories = {
            "Burgers", "Pizza", "Pasta", "Chinese", "Indian",
            "Mexican", "Salads", "Desserts", "Beverages", "Appetizers"
        };

        private static readonly string[] _burgerNames = {
            "Classic Beef Burger", "Cheeseburger Deluxe", "BBQ Bacon Burger",
            "Mushroom Swiss Burger", "Spicy Chicken Burger", "Veggie Supreme",
            "Double Cheeseburger", "Turkey Burger", "Fish Burger", "Avocado Burger"
        };

        private static readonly string[] _pizzaNames = {
            "Margherita Pizza", "Pepperoni Feast", "Hawaiian Paradise",
            "Vegetarian Supreme", "BBQ Chicken Pizza", "Meat Lovers",
            "Four Cheese Pizza", "Mushroom Delight", "Seafood Pizza", "Spicy Sausage"
        };

        private static readonly string[] _pastaNames = {
            "Spaghetti Bolognese", "Fettuccine Alfredo", "Penne Arrabbiata",
            "Carbonara Pasta", "Lasagna Classic", "Mac & Cheese",
            "Pesto Pasta", "Seafood Linguine", "Vegetable Pasta", "Chicken Alfredo"
        };

        private static readonly string[] _dessertNames = {
            "Chocolate Lava Cake", "New York Cheesecake", "Tiramisu",
            "Ice Cream Sundae", "Brownie Delight", "Apple Pie",
            "Chocolate Mousse", "Crème Brûlée", "Fruit Tart", "Panna Cotta"
        };

        private static readonly string[] _imageUrls = {
            "https://images.unsplash.com/photo-1568901346375-23c9450c58cd",
            "https://images.unsplash.com/photo-1571091718767-18b5b1457add",
            "https://images.unsplash.com/photo-1551183053-bf91a1d81141",
            "https://images.unsplash.com/photo-1555949963-aa79dcee981c",
            "https://images.unsplash.com/photo-1563379926898-05f4575a45d8",
            "https://images.unsplash.com/photo-1513104890138-7c749659a591",
            "https://images.unsplash.com/photo-1565958011703-44f9829ba187",
            "https://images.unsplash.com/photo-1481070414801-51fd732d7184",
            "https://images.unsplash.com/photo-1546069901-ba9599a7e63c",
            "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445"
        };

        private static readonly string[] _descriptions = {
            "Deliciously crafted with fresh ingredients and bursting with flavor.",
            "A perfect combination of taste and quality that will satisfy your cravings.",
            "Made with love and premium ingredients for an unforgettable dining experience.",
            "Our signature dish that has been a customer favorite for years.",
            "A hearty meal that combines traditional flavors with modern presentation.",
            "Freshly prepared to order, ensuring maximum flavor and satisfaction.",
            "A balanced meal that offers both nutrition and great taste.",
            "Experience the authentic taste that keeps our customers coming back.",
            "Perfectly seasoned and cooked to perfection for your enjoyment.",
            "A culinary masterpiece that will tantalize your taste buds."
        };

        public static List<FoodItem> GenerateRandomFoodItems(int count)
        {
            var foodItems = new List<FoodItem>();

            for (int i = 0; i < count; i++)
            {
                var category = GetRandomCategory();
                var foodItem = new FoodItem
                {
                    Name = GetRandomNameForCategory(category),
                    Category = category,
                    Price = GetRandomPrice(category),
                    Rating = Math.Round(_random.NextDouble() * 2 + 3, 1), // Random rating between 3.0 and 5.0
                    IsAvailable = _random.Next(2) == 0, // Random true/false
                    Description = _descriptions[_random.Next(_descriptions.Length)],
                    ImageUrl = _imageUrls[_random.Next(_imageUrls.Length)]
                };

                foodItems.Add(foodItem);
            }

            return foodItems;
        }

        private static string GetRandomCategory()
        {
            return _categories[_random.Next(_categories.Length)];
        }

        private static string GetRandomNameForCategory(string category)
        {
            return category switch
            {
                "Burgers" => _burgerNames[_random.Next(_burgerNames.Length)],
                "Pizza" => _pizzaNames[_random.Next(_pizzaNames.Length)],
                "Pasta" => _pastaNames[_random.Next(_pastaNames.Length)],
                "Desserts" => _dessertNames[_random.Next(_dessertNames.Length)],
                "Chinese" => $"Chinese Special {_random.Next(1, 10)}",
                "Indian" => $"Indian Curry {_random.Next(1, 10)}",
                "Mexican" => $"Mexican Fiesta {_random.Next(1, 10)}",
                "Salads" => $"Fresh Salad {_random.Next(1, 10)}",
                "Beverages" => $"Refreshing Drink {_random.Next(1, 10)}",
                "Appetizers" => $"Starter Platter {_random.Next(1, 10)}",
                _ => $"Special Dish {_random.Next(1, 100)}"
            };
        }

        private static decimal GetRandomPrice(string category)
        {
            return category switch
            {
                "Burgers" => Math.Round((decimal)(_random.Next(200, 500) + _random.NextDouble()), 2),
                "Pizza" => Math.Round((decimal)(_random.Next(500, 1200) + _random.NextDouble()), 2),
                "Pasta" => Math.Round((decimal)(_random.Next(300, 700) + _random.NextDouble()), 2),
                "Chinese" => Math.Round((decimal)(_random.Next(250, 600) + _random.NextDouble()), 2),
                "Indian" => Math.Round((decimal)(_random.Next(200, 550) + _random.NextDouble()), 2),
                "Mexican" => Math.Round((decimal)(_random.Next(220, 500) + _random.NextDouble()), 2),
                "Desserts" => Math.Round((decimal)(_random.Next(150, 400) + _random.NextDouble()), 2),
                "Salads" => Math.Round((decimal)(_random.Next(180, 450) + _random.NextDouble()), 2),
                "Beverages" => Math.Round((decimal)(_random.Next(80, 250) + _random.NextDouble()), 2),
                "Appetizers" => Math.Round((decimal)(_random.Next(120, 350) + _random.NextDouble()), 2),
                _ => Math.Round((decimal)(_random.Next(100, 500) + _random.NextDouble()), 2)
            };
        }
    }
}