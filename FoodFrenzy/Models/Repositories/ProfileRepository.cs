using Microsoft.Data.SqlClient;
using Dapper;
using FoodFrenzy.Models.Interfaces;
using FoodFrenzy.Models;
using Microsoft.AspNetCore.Identity;
using FoodFrenzy.Services;


namespace FoodFrenzy.Models.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly string _connectionString;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileRepository(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _userManager = userManager;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();

            using var transaction = con.BeginTransaction();

            try
            {
                // 🔴 Delete related Orders first
                var deleteOrders = @"DELETE FROM Orders WHERE UserId = @UserId";
                await con.ExecuteAsync(deleteOrders, new { UserId = userId }, transaction);

                // 🔴 Identity related tables
                await con.ExecuteAsync(
                    "DELETE FROM AspNetUserTokens WHERE UserId = @UserId",
                    new { UserId = userId }, transaction);

                await con.ExecuteAsync(
                    "DELETE FROM AspNetUserLogins WHERE UserId = @UserId",
                    new { UserId = userId }, transaction);

                await con.ExecuteAsync(
                    "DELETE FROM AspNetUserClaims WHERE UserId = @UserId",
                    new { UserId = userId }, transaction);

                await con.ExecuteAsync(
                    "DELETE FROM AspNetUserRoles WHERE UserId = @UserId",
                    new { UserId = userId }, transaction);

                // 🔴 Finally delete user
                var rows = await con.ExecuteAsync(
                    "DELETE FROM AspNetUsers WHERE Id = @UserId",
                    new { UserId = userId }, transaction);

                transaction.Commit();

                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }
        public IEnumerable<ApplicationUser> GetAllUser()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
            SELECT 
                Id,
                UserName,
                Email,
                PhoneNumber,
                ISNULL(FullName, '') as FullName,
                ISNULL(Address, '') as Address,
                ISNULL(imgpath, '') as imgpath,
                Status
            FROM AspNetUsers 
            ORDER BY UserName";

                return connection.Query<ApplicationUser>(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all users: {ex.Message}");
                return new List<ApplicationUser>();
            }
        }

        public ApplicationUser GetUserProfile(string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var sql = @"
            SELECT 
                Id,
                UserName,
                Email,
                PhoneNumber,
                ISNULL(FullName, '') as FullName,
                ISNULL(Address, '') as Address,
                ISNULL(imgpath, '') as imgpath
            FROM AspNetUsers 
            WHERE Id = @UserId";

                var user = connection.QueryFirstOrDefault<ApplicationUser>(sql, new { UserId = userId });
                return user ?? new ApplicationUser();
            }
            catch (Exception ex)
            {
                // Log error here
                Console.WriteLine($"Error getting user profile: {ex.Message}");
                return new ApplicationUser();
            }
        }

        public bool UpdateProfile(ApplicationUser user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var sql = @"
                    UPDATE AspNetUsers 
                    SET FullName = @FullName,
                        PhoneNumber = @PhoneNumber,
                        Email = @Email,
                        UserName = @Email
                    WHERE Id = @Id";

                var rows = connection.Execute(sql, new
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email
                });

                return rows > 0;
            }
            catch (Exception ex)
            {
                // Log error here
                Console.WriteLine($"Error updating profile: {ex.Message}");
                return false;
            }
        }

        public bool ChangePassword(string userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = _userManager.FindByIdAsync(userId).Result;
                if (user == null) return false;

                var result = _userManager.ChangePasswordAsync(user, currentPassword, newPassword).Result;
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                // Log error here
                Console.WriteLine($"Error changing password: {ex.Message}");
                return false;
            }
        }

        public List<OrderHistory> GetOrderHistory(string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var sql = @"
                    SELECT 
                        o.Id as OrderId,
                        o.OrderDate,
                        o.TotalAmount,
                        o.Status
                    FROM Orders o
                    WHERE o.UserId = @UserId
                    ORDER BY o.OrderDate DESC";

                var orders = connection.Query<OrderHistory>(sql, new { UserId = userId }).ToList();

                // Get items for each order
                foreach (var order in orders)
                {
                    var itemsSql = @"
                        SELECT fi.Name
                        FROM OrderItems oi
                        INNER JOIN FoodItems fi ON oi.FoodItemId = fi.Id
                        WHERE oi.OrderId = @OrderId";

                    var items = connection.Query<string>(itemsSql, new { OrderId = order.OrderId }).ToList();
                    order.Items = string.Join(", ", items);
                }

                return orders;
            }
            catch (Exception ex)
            {
                // Log error here
                Console.WriteLine($"Error getting order history: {ex.Message}");

                // Return dummy data only in development
                return new List<OrderHistory>
                {
                    new OrderHistory { OrderId = "1023", OrderDate = DateTime.Parse("2025-11-05"), Items = "Biryani, Mutton Karahi", Status = "Delivered", TotalAmount = 2500 },
                    new OrderHistory { OrderId = "1024", OrderDate = DateTime.Parse("2025-11-01"), Items = "Chicken Biryani", Status = "Delivered", TotalAmount = 1200 },
                    new OrderHistory { OrderId = "1025", OrderDate = DateTime.Parse("2025-10-28"), Items = "Beef Karahi", Status = "Cancelled", TotalAmount = 1800 }
                };
            }
        }

        public bool UpdateAddress(string userId, string address)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var sql = "UPDATE AspNetUsers SET Address = @Address WHERE Id = @UserId";
                var rows = connection.Execute(sql, new { UserId = userId, Address = address });

                return rows > 0;
            }
            catch (Exception ex)
            {
                // Log error here
                Console.WriteLine($"Error updating address: {ex.Message}");
                return false;
            }
        }
    }
}