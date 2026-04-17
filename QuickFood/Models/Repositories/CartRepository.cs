using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Text.Json;
using FoodFrenzy.Models.Interfaces;
using FoodFrenzy.Models;

namespace FoodFrenzy.Models.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly string _connectionString;

        public CartRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<List<CartItem>> GetCartAsync(string userId, ISession session)
        {
            List<CartItem> cartItems = new List<CartItem>();

            if (!string.IsNullOrEmpty(userId))
            {
                // Get from database for authenticated users
                using var con = new SqlConnection(_connectionString);
                var query = @"
                    SELECT ci.*, fi.Name, fi.ImageUrl 
                    FROM CartItems ci
                    LEFT JOIN FoodItems fi ON ci.FoodItemId = fi.Id
                    WHERE ci.UserId = @UserId";

                var dbItems = await con.QueryAsync<CartItem>(query, new { UserId = userId });
                cartItems = dbItems.ToList();
            }
            else
            {
                // Get from session for guest users
                var sessionCart = session.GetString("Cart");
                if (!string.IsNullOrEmpty(sessionCart))
                {
                    cartItems = JsonSerializer.Deserialize<List<CartItem>>(sessionCart) ?? new List<CartItem>();
                }
            }

            return cartItems ?? new List<CartItem>();
        }

        public async Task AddToCartAsync(string userId, int foodItemId, string name, decimal price, string imageUrl, ISession session)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                // Add to database for authenticated users
                using var con = new SqlConnection(_connectionString);

                // Check if item already exists in cart
                var existingItem = await con.QueryFirstOrDefaultAsync<CartItem>(
                    "SELECT * FROM CartItems WHERE UserId = @UserId AND FoodItemId = @FoodItemId",
                    new { UserId = userId, FoodItemId = foodItemId }
                );

                if (existingItem != null)
                {
                    // Update quantity
                    await con.ExecuteAsync(
                        "UPDATE CartItems SET Quantity = Quantity + 1 WHERE Id = @Id",
                        new { Id = existingItem.Id }
                    );
                }
                else
                {
                    // Insert new item
                    await con.ExecuteAsync(
                        @"INSERT INTO CartItems (UserId, FoodItemId, Quantity, Price, Name, ImageUrl) 
                          VALUES (@UserId, @FoodItemId, 1, @Price, @Name, @ImageUrl)",
                        new
                        {
                            UserId = userId,
                            FoodItemId = foodItemId,
                            Price = price,
                            Name = name,
                            ImageUrl = imageUrl
                        }
                    );
                }
            }
            else
            {
                // Add to session for guest users
                var cart = await GetCartAsync(null, session);

                // Generate a temporary ID for session items if not exists
                var existingItem = cart.FirstOrDefault(item => item.FoodItemId == foodItemId);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    // Generate unique ID for session items
                    var maxId = cart.Count > 0 ? cart.Max(item => item.Id) : 0;
                    cart.Add(new CartItem
                    {
                        Id = maxId + 1, // Temporary unique ID for session
                        FoodItemId = foodItemId,
                        Name = name,
                        Price = price,
                        ImageUrl = imageUrl,
                        Quantity = 1,
                        UserId = null
                    });
                }

                // Save to session
                var cartJson = JsonSerializer.Serialize(cart);
                session.SetString("Cart", cartJson);
                await session.CommitAsync();
            }
        }

        public async Task UpdateCartItemAsync(string userId, int cartItemId, int quantity, ISession session)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                // Update in database
                using var con = new SqlConnection(_connectionString);
                if (quantity <= 0)
                {
                    await con.ExecuteAsync(
                        "DELETE FROM CartItems WHERE Id = @Id AND UserId = @UserId",
                        new { Id = cartItemId, UserId = userId }
                    );
                }
                else
                {
                    await con.ExecuteAsync(
                        "UPDATE CartItems SET Quantity = @Quantity WHERE Id = @Id AND UserId = @UserId",
                        new { Id = cartItemId, Quantity = quantity, UserId = userId }
                    );
                }
            }
            else
            {
                // Update in session - FIXED: Use the temporary ID properly
                var cart = await GetCartAsync(null, session);
                var item = cart.FirstOrDefault(item => item.Id == cartItemId);

                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        cart.Remove(item);
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }

                    // Save updated cart to session
                    var cartJson = JsonSerializer.Serialize(cart);
                    session.SetString("Cart", cartJson);
                    await session.CommitAsync();
                }
            }
        }

        public async Task RemoveCartItemAsync(string userId, int cartItemId, ISession session)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                // Remove from database
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    "DELETE FROM CartItems WHERE Id = @Id AND UserId = @UserId",
                    new { Id = cartItemId, UserId = userId }
                );
            }
            else
            {
                // Remove from session
                var cart = await GetCartAsync(null, session);
                var item = cart.FirstOrDefault(item => item.Id == cartItemId);

                if (item != null)
                {
                    cart.Remove(item);
                }

                // Save updated cart to session
                var cartJson = JsonSerializer.Serialize(cart);
                session.SetString("Cart", cartJson);
                await session.CommitAsync();
            }
        }

        public async Task ClearCartAsync(string userId, ISession session)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                // Clear from database
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    "DELETE FROM CartItems WHERE UserId = @UserId",
                    new { UserId = userId }
                );
            }
            else
            {
                // Clear from session
                session.Remove("Cart");
                await session.CommitAsync();
            }
        }

        public async Task MergeCartsAsync(string userId, ISession session)
        {
            var sessionCart = session.GetString("Cart");
            if (!string.IsNullOrEmpty(sessionCart) && !string.IsNullOrEmpty(userId))
            {
                var cartItems = JsonSerializer.Deserialize<List<CartItem>>(sessionCart) ?? new List<CartItem>();

                using var con = new SqlConnection(_connectionString);

                foreach (var item in cartItems)
                {
                    // Check if item exists in database cart
                    var existingItem = await con.QueryFirstOrDefaultAsync<CartItem>(
                        "SELECT * FROM CartItems WHERE UserId = @UserId AND FoodItemId = @FoodItemId",
                        new { UserId = userId, FoodItemId = item.FoodItemId }
                    );

                    if (existingItem != null)
                    {
                        // Update quantity
                        await con.ExecuteAsync(
                            "UPDATE CartItems SET Quantity = Quantity + @Quantity WHERE Id = @Id",
                            new { Id = existingItem.Id, Quantity = item.Quantity }
                        );
                    }
                    else
                    {
                        // Insert new item
                        await con.ExecuteAsync(
                            @"INSERT INTO CartItems (UserId, FoodItemId, Quantity, Price, Name, ImageUrl) 
                              VALUES (@UserId, @FoodItemId, @Quantity, @Price, @Name, @ImageUrl)",
                            new
                            {
                                UserId = userId,
                                FoodItemId = item.FoodItemId,
                                Quantity = item.Quantity,
                                Price = item.Price,
                                Name = item.Name,
                                ImageUrl = item.ImageUrl
                            }
                        );
                    }
                }

                // Clear session cart after merging
                session.Remove("Cart");
                await session.CommitAsync();
            }
        }
    }
}