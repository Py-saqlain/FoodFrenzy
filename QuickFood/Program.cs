using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using FoodFrenzy.Data;
using FoodFrenzy.Hubs;
using FoodFrenzy.Models;
using FoodFrenzy.Models.Interfaces;
using FoodFrenzy.Models.Repositories;
using FoodFrenzy.Models.Services;
using FoodFrenzy.Repositories;
using FoodFrenzy.Services;
using EmailSettings = FoodFrenzy.Services.EmailSettings;

namespace FoodFrenzy
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Add try-catch to see the actual error
            try
            {
                Console.WriteLine("🚀 Starting FoodFrenzy application...");

                var builder = WebApplication.CreateBuilder(args);

                // Add logging to console for debugging
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();
                builder.Logging.AddDebug();
                builder.Logging.SetMinimumLevel(LogLevel.Debug);

                Console.WriteLine("📋 Loading configuration...");

                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

                Console.WriteLine($"✅ Connection string found: {connectionString}");

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(connectionString,
                        sqlServerOptions =>
                        {
                            sqlServerOptions.EnableRetryOnFailure(
                                maxRetryCount: 3,
                                maxRetryDelay: TimeSpan.FromSeconds(5),
                                errorNumbersToAdd: null);
                            sqlServerOptions.CommandTimeout(60);
                        });
                    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
                    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
                });

                builder.Services.AddDatabaseDeveloperPageExceptionFilter();

                Console.WriteLine("✅ Database context configured");

                // Configure EmailSettings
                builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
                Console.WriteLine("✅ Email settings configured");

                // Register services
                builder.Services.AddScoped<IFoodItemRepository, FoodItemRepository>();
                builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
                builder.Services.AddScoped<ICartRepository, CartRepository>();
                builder.Services.AddScoped<IOrderRepository, OrderRepository>();
                Console.WriteLine("✅ Repositories registered");

                // Add IEmailSender implementation
                builder.Services.AddTransient<IEmailSender, RealEmailSender>();
                Console.WriteLine("✅ Email sender configured");

                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("AdminOnly", policy =>
                        policy.RequireRole("Admin"));
                    options.AddPolicy("AdminOrUser", policy =>
                        policy.RequireRole("Admin", "User"));
                    options.AddPolicy("RestaurantOwner", policy =>
                        policy.RequireRole("RestaurantOwner"));
                    options.AddPolicy("AdminOrRestaurantOwner", policy =>
                        policy.RequireRole("Admin", "RestaurantOwner"));
                });


                // Add Session services
                builder.Services.AddDistributedMemoryCache();
                builder.Services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(30);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.Name = "FoodFrenzy.Session";
                });
                Console.WriteLine("✅ Session services configured");

                // Identity configuration
                builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequiredLength = 6;
                    options.Password.RequiredUniqueChars = 1;
                    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
                Console.WriteLine("✅ Identity services configured");

                builder.Services.AddControllersWithViews(options =>
                {
                    // Add global filters if needed
                }).AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

                builder.Services.AddSignalR();
                builder.Services.AddRazorPages();
                builder.Services.AddHttpContextAccessor();
                Console.WriteLine("✅ MVC services configured");

                // Configure AntiForgery
                builder.Services.AddAntiforgery(options =>
                {
                    options.HeaderName = "X-CSRF-TOKEN";
                    options.Cookie.Name = "X-CSRF-TOKEN";
                    options.FormFieldName = "__RequestVerificationToken";
                    options.SuppressXFrameOptionsHeader = false;
                });
                Console.WriteLine("✅ Anti-forgery configured");

                var app = builder.Build();
                Console.WriteLine("✅ Application built successfully");

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseMigrationsEndPoint();
                    app.UseDeveloperExceptionPage();
                    Console.WriteLine("✅ Development environment configured");
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                    Console.WriteLine("✅ Production environment configured");
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseSession();
                app.UseAuthentication();
                app.UseAuthorization();
                Console.WriteLine("✅ HTTP pipeline configured");

                // Initialize database and seed data
                await InitializeDatabaseAndSeedData(app);
                Console.WriteLine("✅ Database initialized and seeded");

                app.UseWebSockets();
                app.MapHub<NotificationHub>("/notificationHub");

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                app.MapRazorPages();
                Console.WriteLine("✅ Routes configured");

                Console.WriteLine("🎉 Application is starting...");
                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 FATAL ERROR: {ex.Message}");
                Console.WriteLine($"📋 Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"🔍 Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private static async Task InitializeDatabaseAndSeedData(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Initializing database...");

                // Database migration
                var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

                if (await context.Database.CanConnectAsync())
                {
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        await context.Database.MigrateAsync();
                        logger.LogInformation("Applied database migrations");
                    }
                }
                else
                {
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migrated successfully");
                }

                // Create roles
                await CreateRoles(serviceProvider);
                logger.LogInformation("Roles created/verified");

                // Create admin user
                await CreateAdminUser(serviceProvider);
                logger.LogInformation("Admin user created/verified");

                // Seed food items if database is empty
                await SeedFoodItems(serviceProvider);
                logger.LogInformation("Food items seeding completed");

                logger.LogInformation("Database initialization and seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }

        private static async Task CreateRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = { "Admin", "User", "RestaurantOwner" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task CreateAdminUser(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var adminEmail = "admin@FoodFrenzy.com";
            var adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Admin User",
                    Address = "Admin Default Address",
                    imgpath = "/uploads/default.png"
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    Console.WriteLine($"❌ Failed to create admin user: {errors}");
                }
            }
        }

        private static async Task SeedFoodItems(IServiceProvider serviceProvider)
        {
            try
            {
                var foodRepository = serviceProvider.GetRequiredService<IFoodItemRepository>();

                // Check if database has any food items
                var existingItems = foodRepository.GetAllFoodItems();

                if (!existingItems.Any())
                {
                    Console.WriteLine("🌱 Database is empty. Seeding with random food items...");

                    // Use the FoodItemSeeder from FoodFrenzy.Services namespace
                    var randomItems = FoodItemSeeder.GenerateRandomFoodItems(20); // Generate 20 items

                    // Add each item to database
                    int addedCount = 0;
                    foreach (var item in randomItems)
                    {
                        var result = foodRepository.AddFoodItem(item);
                        if (result)
                        {
                            addedCount++;
                            Console.WriteLine($"✅ Added: {item.Name} - Rs {item.Price}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Failed to add: {item.Name}");
                        }
                    }

                    Console.WriteLine($"🎉 Successfully seeded {addedCount} random food items!");
                }
                else
                {
                    Console.WriteLine($"📊 Database already has {existingItems.Count()} food items. No seeding needed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding food items: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                // If FoodItemSeeder doesn't exist yet, add some basic items manually
                if (ex.Message.Contains("FoodItemSeeder"))
                {
                    Console.WriteLine("🔄 Adding basic food items manually...");
                    await AddBasicFoodItemsManually(serviceProvider);
                }
            }
        }

        private static async Task AddBasicFoodItemsManually(IServiceProvider serviceProvider)
        {
            var foodRepository = serviceProvider.GetRequiredService<IFoodItemRepository>();

            var basicItems = new List<FoodItem>
            {
                new FoodItem
                {
                    Name = "Classic Burger",
                    Category = "Burgers",
                    Price = 299.99m,
                    Rating = 4.5,
                    IsAvailable = true,
                    Description = "Juicy beef patty with fresh vegetables",
                    ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd"
                },
                new FoodItem
                {
                    Name = "Margherita Pizza",
                    Category = "Pizza",
                    Price = 499.99m,
                    Rating = 4.7,
                    IsAvailable = true,
                    Description = "Classic pizza with tomato and mozzarella",
                    ImageUrl = "https://images.unsplash.com/photo-1604068549290-dea0e4a305ca"
                },
                new FoodItem
                {
                    Name = "Chicken Fried Rice",
                    Category = "Chinese",
                    Price = 349.99m,
                    Rating = 4.3,
                    IsAvailable = true,
                    Description = "Stir-fried rice with chicken and vegetables",
                    ImageUrl = "https://images.unsplash.com/photo-1603133872878-684f208fb84b"
                },
                new FoodItem
                {
                    Name = "Chocolate Cake",
                    Category = "Desserts",
                    Price = 199.99m,
                    Rating = 4.8,
                    IsAvailable = true,
                    Description = "Rich and moist chocolate cake",
                    ImageUrl = "https://images.unsplash.com/photo-1578985545062-69928b1d9587"
                },
                new FoodItem
                {
                    Name = "Fresh Salad",
                    Category = "Salads",
                    Price = 249.99m,
                    Rating = 4.2,
                    IsAvailable = true,
                    Description = "Healthy mix of fresh vegetables",
                    ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c"
                }
            };

            int addedCount = 0;
            foreach (var item in basicItems)
            {
                if (foodRepository.AddFoodItem(item))
                {
                    addedCount++;
                    Console.WriteLine($"✅ Added basic item: {item.Name}");
                }
            }

            Console.WriteLine($"✅ Added {addedCount} basic food items");
        }
    }
}