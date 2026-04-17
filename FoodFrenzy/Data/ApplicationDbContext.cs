using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FoodFrenzy.Models;

namespace FoodFrenzy.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FoodItem> FoodItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(100);
                entity.Property(e => e.imgpath).HasMaxLength(1000);
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("pending");

                // Configure relationships
                entity.HasMany(u => u.Orders)
                      .WithOne(o => o.User)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.CartItems)
                      .WithOne(ci => ci.User)
                      .HasForeignKey(ci => ci.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Order
            builder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.OrderNumber)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.TrackingNumber)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.CustomerName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.CustomerEmail)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.CustomerPhone)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(e => e.DeliveryAddress)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.City)
                      .HasMaxLength(100);

                entity.Property(e => e.ZipCode)
                      .HasMaxLength(20);

                entity.Property(e => e.DeliveryInstructions)
                      .HasMaxLength(500);

                entity.Property(e => e.PaymentMethod)
                      .HasMaxLength(50);

                entity.Property(e => e.DeliveryMethod)
                      .HasMaxLength(50);

                entity.Property(e => e.Status)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasDefaultValue("Pending");

                // Decimal properties
                entity.Property(e => e.Subtotal)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.DeliveryFee)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.ServiceFee)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.Tax)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.Total)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.OrderDate)
                      .IsRequired()
                      .HasDefaultValueSql("GETDATE()");

                // Relationships
                entity.HasOne(o => o.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(o => o.OrderItems)
                      .WithOne(oi => oi.Order)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Add indexes for better performance
                entity.HasIndex(o => o.OrderNumber).IsUnique();
                entity.HasIndex(o => o.TrackingNumber).IsUnique();
                entity.HasIndex(o => o.UserId);
                entity.HasIndex(o => o.OrderDate);
                entity.HasIndex(o => o.Status);
            });

            // Configure FoodItem
            builder.Entity<FoodItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.Category)
                      .HasMaxLength(50);

                entity.Property(e => e.Price)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.Rating)
                      .HasColumnType("float")
                      .HasDefaultValue(0.0);

                entity.Property(e => e.ImageUrl)
                      .HasMaxLength(500);

                entity.Property(e => e.IsAvailable)
                      .HasDefaultValue(true);

                // Configure relationships with CASCADE DELETE
                entity.HasMany(f => f.OrderItems)
                      .WithOne(oi => oi.FoodItem)
                      .HasForeignKey(oi => oi.FoodItemId)
                      .OnDelete(DeleteBehavior.Cascade); // Changed from Restrict to Cascade

                entity.HasMany(f => f.CartItems)
                      .WithOne(ci => ci.FoodItem)
                      .HasForeignKey(ci => ci.FoodItemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CartItem
            builder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                      .IsRequired()
                      .HasMaxLength(450);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Price)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(e => e.ImageUrl)
                      .HasMaxLength(500);

                entity.Property(e => e.Quantity)
                      .IsRequired()
                      .HasDefaultValue(1);

                entity.Property(e => e.AddedAt)
                      .IsRequired()
                      .HasDefaultValueSql("GETDATE()");

                // Configure relationships
                entity.HasOne(ci => ci.User)
                      .WithMany(u => u.CartItems)
                      .HasForeignKey(ci => ci.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.FoodItem)
                      .WithMany(f => f.CartItems)
                      .HasForeignKey(ci => ci.FoodItemId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Add index for better performance
                entity.HasIndex(ci => ci.UserId);
                entity.HasIndex(ci => ci.FoodItemId);
            });

            // Configure OrderItem
            builder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FoodItemName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.ImageUrl)
                      .HasMaxLength(500);

                entity.Property(e => e.Quantity)
                      .IsRequired()
                      .HasDefaultValue(1);

                entity.Property(e => e.Price)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                // Configure relationships with CASCADE DELETE
                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.FoodItem)
                      .WithMany(f => f.OrderItems)
                      .HasForeignKey(oi => oi.FoodItemId)
                      .OnDelete(DeleteBehavior.Cascade); // Changed from Restrict to Cascade

                // Add index for better performance
                entity.HasIndex(oi => oi.OrderId);
                entity.HasIndex(oi => oi.FoodItemId);
            });

            // Configure ContactMessage
            builder.Entity<ContactMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName)
                      .IsRequired()
                      .HasMaxLength(50);
                entity.Property(e => e.LastName)
                      .IsRequired()
                      .HasMaxLength(50);
                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(100);

                // Phone is nullable based on your model
                entity.Property(e => e.Phone)
                      .HasMaxLength(20);

                entity.Property(e => e.Subject)
                      .IsRequired()
                      .HasMaxLength(200);
                entity.Property(e => e.Message)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("GETDATE()");
            });
        }
    }
    }
