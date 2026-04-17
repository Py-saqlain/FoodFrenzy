using Microsoft.Data.SqlClient;
using Dapper;
using FoodFrenzy.Models;
using FoodFrenzy.Repositories;

namespace FoodFrenzy.Models.Repositories
{
    public class FoodItemRepository : IFoodItemRepository
    {
        private readonly string _connectionString;

        public FoodItemRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IEnumerable<FoodItem> GetAllFoodItems()
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
                SELECT Id, Name, Category, Price, Rating, IsAvailable, Description, ImageUrl 
                FROM FoodItems 
                ORDER BY Name";

            return connection.Query<FoodItem>(sql);
        }

        public IEnumerable<FoodItem> GetTopThreeFoodItems()
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
                SELECT TOP (3)
                    Id, Name, Category, Price, Rating, IsAvailable, Description, ImageUrl
                FROM FoodItems
                WHERE IsAvailable = 1
                ORDER BY Rating DESC";

            return connection.Query<FoodItem>(sql);
        }

        public FoodItem GetFoodItemById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
                SELECT Id, Name, Category, Price, Rating, IsAvailable, Description, ImageUrl 
                FROM FoodItems 
                WHERE Id = @Id";

            return connection.QuerySingleOrDefault<FoodItem>(sql, new { Id = id });
        }

        public bool AddFoodItem(FoodItem foodItem)
        {
            try
            {
                Console.WriteLine($"Repository: Adding food item - Name: {foodItem.Name}, Price: {foodItem.Price}, Rating: {foodItem.Rating}");

                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO FoodItems (Name, Category, Price, Rating, IsAvailable, Description, ImageUrl)
                    VALUES (@Name, @Category, @Price, @Rating, @IsAvailable, @Description, @ImageUrl)";

                var parameters = new
                {
                    Name = foodItem.Name ?? string.Empty,
                    Category = foodItem.Category ?? string.Empty,
                    foodItem.Price,
                    Rating = (double)foodItem.Rating,
                    foodItem.IsAvailable,
                    Description = foodItem.Description ?? string.Empty,
                    ImageUrl = foodItem.ImageUrl ?? string.Empty
                };

                Console.WriteLine($"Repository: SQL parameters - Rating: {parameters.Rating}");

                var rowsAffected = connection.Execute(sql, parameters);
                Console.WriteLine($"Repository: Rows affected: {rowsAffected}");

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Repository Error: {ex.Message}");
                return false;
            }
        }

        public bool UpdateFoodItem(FoodItem foodItem)
        {
            try
            {
                Console.WriteLine($"Repository: Updating food item ID={foodItem.Id}");

                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE FoodItems 
                    SET Name = @Name, Category = @Category, Price = @Price, 
                        Rating = @Rating, IsAvailable = @IsAvailable, 
                        Description = @Description, ImageUrl = @ImageUrl
                    WHERE Id = @Id";

                var parameters = new
                {
                    foodItem.Id,
                    Name = foodItem.Name ?? string.Empty,
                    Category = foodItem.Category ?? string.Empty,
                    foodItem.Price,
                    Rating = (double)foodItem.Rating,
                    foodItem.IsAvailable,
                    Description = foodItem.Description ?? string.Empty,
                    ImageUrl = foodItem.ImageUrl ?? string.Empty
                };

                var rowsAffected = connection.Execute(sql, parameters);
                Console.WriteLine($"Repository: Update rows affected: {rowsAffected}");

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Repository Update Error: {ex.Message}");
                return false;
            }
        }

        public bool DeleteFoodItem(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            try
            {
                Console.WriteLine($"Repository: Deleting food item ID: {id}");
                connection.Open();

                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. First, check if food item exists
                    var checkSql = "SELECT COUNT(*) FROM FoodItems WHERE Id = @Id";
                    var exists = connection.ExecuteScalar<int>(checkSql, new { Id = id }, transaction) > 0;

                    if (!exists)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Food item with ID {id} not found.");
                        return false;
                    }

                    // 2. Delete related OrderItems
                    var deleteOrderItemsSql = "DELETE FROM OrderItems WHERE FoodItemId = @FoodItemId";
                    var orderItemsDeleted = connection.Execute(deleteOrderItemsSql, new { FoodItemId = id }, transaction);
                    Console.WriteLine($"Deleted {orderItemsDeleted} OrderItems");

                    // 3. Delete related CartItems
                    var deleteCartItemsSql = "DELETE FROM CartItems WHERE FoodItemId = @FoodItemId";
                    var cartItemsDeleted = connection.Execute(deleteCartItemsSql, new { FoodItemId = id }, transaction);
                    Console.WriteLine($"Deleted {cartItemsDeleted} CartItems");

                    // 4. Now delete the FoodItem
                    var deleteFoodItemSql = "DELETE FROM FoodItems WHERE Id = @Id";
                    var rowsAffected = connection.Execute(deleteFoodItemSql, new { Id = id }, transaction);

                    if (rowsAffected > 0)
                    {
                        transaction.Commit();
                        Console.WriteLine($"Successfully deleted food item ID: {id}");
                        return true;
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.WriteLine($"No food item found with ID: {id}");
                        return false;
                    }
                }
                catch (SqlException sqlEx)
                {
                    transaction.Rollback();
                    Console.WriteLine($"SQL Error during delete: {sqlEx.Message}");
                    Console.WriteLine($"Error Number: {sqlEx.Number}, Line: {sqlEx.LineNumber}");
                    return false;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Error during delete: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
                return false;
            }
        }

        // Helper method to check if food item has orders
        public bool FoodItemHasOrders(int foodItemId)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = "SELECT COUNT(*) FROM OrderItems WHERE FoodItemId = @FoodItemId";
            var count = connection.ExecuteScalar<int>(sql, new { FoodItemId = foodItemId });
            return count > 0;
        }
    }
}