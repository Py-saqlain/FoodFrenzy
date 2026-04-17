using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Respawn;
using Xunit;

namespace FoodFrenzy.IntegrationTests.Database
{
    public class TestDatabaseFixture : IAsyncLifetime
    {
        private readonly IConfiguration _configuration;
        private Respawner _respawner;
        public string ConnectionString { get; private set; }

        public TestDatabaseFixture()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            ConnectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task InitializeAsync()
        {
            await CreateDatabaseIfNotExists();
            await CreateTablesIfNotExist();
            await InitializeRespawner();
        }

        private async Task CreateDatabaseIfNotExists()
        {
            // Extract database name from connection string
            var builder = new SqlConnectionStringBuilder(ConnectionString);
            var databaseName = builder.InitialCatalog;
            builder.InitialCatalog = "master";

            using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            var checkDbSql = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}'";
            var exists = await connection.ExecuteScalarAsync<int>(checkDbSql) > 0;

            if (!exists)
            {
                var createDbSql = $"CREATE DATABASE [{databaseName}]";
                await connection.ExecuteAsync(createDbSql);
                Console.WriteLine($"Created database: {databaseName}");
            }

            builder.InitialCatalog = databaseName;
            ConnectionString = builder.ConnectionString;
        }

        private async Task CreateTablesIfNotExist()
        {
            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            var createTablesSql = @"
                -- Create FoodItems table
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
                END

                -- Create Orders table
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' and xtype='U')
                BEGIN
                    CREATE TABLE Orders (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        UserId NVARCHAR(450) NOT NULL,
                        OrderNumber NVARCHAR(50) UNIQUE NOT NULL,
                        Subtotal DECIMAL(10,2) NOT NULL,
                        DeliveryFee DECIMAL(10,2) NOT NULL,
                        ServiceFee DECIMAL(10,2) NOT NULL,
                        Tax DECIMAL(10,2) NOT NULL,
                        Total DECIMAL(10,2) NOT NULL,
                        CustomerName NVARCHAR(100) NOT NULL,
                        CustomerEmail NVARCHAR(100) NOT NULL,
                        CustomerPhone NVARCHAR(20) NOT NULL,
                        DeliveryAddress NVARCHAR(200) NOT NULL,
                        City NVARCHAR(100),
                        ZipCode NVARCHAR(20),
                        DeliveryInstructions NVARCHAR(500),
                        PaymentMethod NVARCHAR(50),
                        DeliveryMethod NVARCHAR(50),
                        OrderDate DATETIME2 NOT NULL,
                        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                        TrackingNumber NVARCHAR(100) UNIQUE
                    )
                END

                -- Create OrderItems table
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' and xtype='U')
                BEGIN
                    CREATE TABLE OrderItems (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        OrderId INT NOT NULL,
                        FoodItemId INT NOT NULL,
                        FoodItemName NVARCHAR(100) NOT NULL,
                        Price DECIMAL(10,2) NOT NULL,
                        Quantity INT NOT NULL,
                        ImageUrl NVARCHAR(500),
                        FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
                    )
                END

                -- Create CartItems table
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CartItems' and xtype='U')
                BEGIN
                    CREATE TABLE CartItems (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        FoodItemId INT NOT NULL,
                        Quantity INT NOT NULL,
                        UserId NVARCHAR(450) NOT NULL
                    )
                END";

            await connection.ExecuteAsync(createTablesSql);
            Console.WriteLine("Tables created/verified successfully");
        }

        private async Task InitializeRespawner()
        {
            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                TablesToIgnore = new Respawn.Graph.Table[] { },
                SchemasToExclude = new[] { "sys", "dbo" },
                DbAdapter = DbAdapter.SqlServer
            });
        }

        public async Task ResetDatabaseAsync()
        {
            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }

        public async Task DisposeAsync()
        {
            // Optional: Drop database after all tests
            // var builder = new SqlConnectionStringBuilder(ConnectionString);
            // var databaseName = builder.InitialCatalog;
            // builder.InitialCatalog = "master";
            // 
            // using var connection = new SqlConnection(builder.ConnectionString);
            // await connection.OpenAsync();
            // await connection.ExecuteAsync($"DROP DATABASE IF EXISTS [{databaseName}]");
        }
    }
}