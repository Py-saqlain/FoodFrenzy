using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace FoodFrenzy.Tests.TestHelpers
{
    public static class DatabaseTestHelper
    {
        public static void ResetDatabase(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Disable foreign key constraints
            connection.Execute("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

            // Delete data from all tables
            connection.Execute("EXEC sp_MSforeachtable 'DELETE FROM ?'");

            // Re-enable foreign key constraints
            connection.Execute("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
        }

        public static void SeedTestData(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);

            // Insert base test data
            var sql = @"
                INSERT INTO FoodItems (Name, Category, Price, Rating, IsAvailable, Description, ImageUrl)
                VALUES 
                    ('Test Burger', 'Fast Food', 9.99, 4.5, 1, 'Test burger', 'test.jpg'),
                    ('Test Pizza', 'Italian', 12.99, 4.7, 1, 'Test pizza', 'test.jpg'),
                    ('Test Salad', 'Healthy', 7.99, 4.2, 0, 'Test salad', 'test.jpg')";

            connection.Execute(sql);
        }
    }
}