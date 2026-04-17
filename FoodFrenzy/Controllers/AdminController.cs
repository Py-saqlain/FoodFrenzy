using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FoodFrenzy.Hubs;
using FoodFrenzy.Models;
using FoodFrenzy.Models.Interfaces;
using FoodFrenzy.Models.ViewModels;
using FoodFrenzy.Repositories;

namespace FoodFrenzy.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IFoodItemRepository _foodRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IOrderRepository _OrderRepository;
        private readonly ILogger<AdminController> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AdminController(
            IFoodItemRepository foodRepository,
            IProfileRepository profileRepository,
            IOrderRepository OrderRepository,
            ILogger<AdminController> logger,
            IHubContext<NotificationHub> hubContext)
        {
            _foodRepository = foodRepository;
            _profileRepository = profileRepository;
            _OrderRepository = OrderRepository;
            _logger = logger;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            try
            {
                var foodItems = _foodRepository.GetAllFoodItems();
                var users = _profileRepository.GetAllUser();
                var orders = _OrderRepository.GetAllOrders();

                ViewBag.Users = users;
                ViewBag.Orders = orders;

                // Create a default/empty view model for the sales chart
                ViewBag.SalesViewModel = new WeeklySalesViewModel
                {
                    WeeklyData = new List<DailySalesData>(),
                    TopProducts = new List<TopProduct>(),
                    TotalRevenue = 0,
                    TotalOrders = 0,
                    AverageOrderValue = 0,
                    BestDay = "No data"
                };

                return View(foodItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "Error loading data. Please try again.";
                return View(new List<FoodItem>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(FoodItem foodItem)
        {
            try
            {
                _logger.LogInformation($"Creating food item: {foodItem.Name}");

                if (ModelState.IsValid)
                {
                    var result = _foodRepository.AddFoodItem(foodItem);
                    if (result)
                    {
                        TempData["SuccessMessage"] = $"Food item '{foodItem.Name}' added successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to add food item. Please try again.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please fill all required fields correctly.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating food item: {ex.Message}");
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                var foodItem = _foodRepository.GetFoodItemById(id);
                if (foodItem == null)
                {
                    return NotFound();
                }
                return PartialView("_UpdateProduct", foodItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit form for ID: {id}");
                return StatusCode(500, "Error loading form");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(FoodItem foodItem)
        {
            try
            {
                _logger.LogInformation($"Updating food item ID: {foodItem.Id}");

                if (ModelState.IsValid)
                {
                    var result = _foodRepository.UpdateFoodItem(foodItem);
                    if (result)
                    {
                        TempData["SuccessMessage"] = $"Food item '{foodItem.Name}' updated successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update food item. Please try again.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please fill all required fields correctly.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating food item: {ex.Message}");
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                var foodItem = _foodRepository.GetFoodItemById(id);
                if (foodItem == null)
                {
                    TempData["ErrorMessage"] = "Food item not found.";
                    return RedirectToAction("Index");
                }

                // Check if food item has orders
                bool hasOrders = _foodRepository.FoodItemHasOrders(id);

                if (hasOrders)
                {
                    // Store warning in TempData
                    TempData["WarningMessage"] = $"Warning: '{foodItem.Name}' has been ordered by customers. Deleting it will remove it from order history.";

                    // Store the food item ID for confirmation
                    TempData["DeleteFoodItemId"] = id;
                    TempData["DeleteFoodItemName"] = foodItem.Name;

                    return RedirectToAction("Index");
                }

                // If no orders, proceed with deletion
                var result = _foodRepository.DeleteFoodItem(id);
                if (result)
                {
                    TempData["SuccessMessage"] = $"Food item '{foodItem.Name}' deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete food item.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting food item ID: {id}");
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmDelete(int id)
        {
            try
            {
                var foodItem = _foodRepository.GetFoodItemById(id);
                if (foodItem == null)
                {
                    TempData["ErrorMessage"] = "Food item not found.";
                    return RedirectToAction("Index");
                }

                var result = _foodRepository.DeleteFoodItem(id);
                if (result)
                {
                    TempData["SuccessMessage"] = $"Food item '{foodItem.Name}' deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete food item.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error confirming delete for food item ID: {id}");
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                // Prevent admin deleting himself
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (id == currentUserId)
                {
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction("Index");
                }

                var result = await _profileRepository.DeleteUserAsync(id);
                TempData[result ? "SuccessMessage" : "ErrorMessage"] =
                    result ? "User deleted successfully!" : "Failed to delete user.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user ID: {id}");
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

    

        [HttpGet]
        public IActionResult GetWeeklySalesData(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                if (!startDate.HasValue)
                    startDate = DateTime.Now.AddDays(-30);
                if (!endDate.HasValue)
                    endDate = DateTime.Now;

                // Ensure end date is not before start date
                if (endDate < startDate)
                    endDate = startDate.Value.AddDays(30);

                // Get orders within date range
                var orders = _OrderRepository.GetOrdersByDateRange(startDate.Value, endDate.Value);

                // Group by day
                var dailySales = orders
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new DailySalesData
                    {
                        Date = g.Key,
                        DayName = g.Key.DayOfWeek.ToString(),
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.Total),  // Changed from TotalAmount to Total
                        AverageOrderValue = g.Average(o => o.Total)  // Changed from TotalAmount to Total
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Fill in missing days
                var allDays = new List<DailySalesData>();
                for (var date = startDate.Value; date <= endDate.Value; date = date.AddDays(1))
                {
                    var existingDay = dailySales.FirstOrDefault(d => d.Date.Date == date.Date);
                    if (existingDay != null)
                    {
                        allDays.Add(existingDay);
                    }
                    else
                    {
                        allDays.Add(new DailySalesData
                        {
                            Date = date,
                            DayName = date.DayOfWeek.ToString(),
                            Revenue = 0,
                            OrderCount = 0,
                            AverageOrderValue = 0
                        });
                    }
                }

                // Get top products
                var topProducts = orders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.FoodItem?.Name ?? "Unknown")
                    .Select(g => new TopProduct
                    {
                        Name = g.Key,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Price * oi.Quantity),
                        OrderCount = g.Select(oi => oi.OrderId).Distinct().Count()
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(5)
                    .ToList();

                // Calculate overall statistics
                var totalRevenue = allDays.Sum(d => d.Revenue);
                var totalOrders = allDays.Sum(d => d.OrderCount);
                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
                var bestDay = allDays.OrderByDescending(d => d.Revenue).FirstOrDefault();

                var viewModel = new WeeklySalesViewModel
                {
                    WeeklyData = allDays,
                    TopProducts = topProducts,
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = averageOrderValue,
                    BestDay = bestDay != null && bestDay.Revenue > 0 ? bestDay.DayName : "No data"
                };

                return Json(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly sales data");
                return Json(new WeeklySalesViewModel
                {
                    WeeklyData = new List<DailySalesData>(),
                    TopProducts = new List<TopProduct>(),
                    TotalRevenue = 0,
                    TotalOrders = 0,
                    AverageOrderValue = 0,
                    BestDay = "Error"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            try
            {
                var order = await _OrderRepository.GetOrderWithItemsAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                return Json(new
                {
                    success = true,
                    order = new
                    {
                        id = order.Id,
                        orderNumber = order.OrderNumber,
                        customerName = order.CustomerName,
                        customerEmail = order.CustomerEmail,
                        customerPhone = order.CustomerPhone,
                        deliveryAddress = order.DeliveryAddress,
                        status = order.Status,
                        total = order.Total,  // Changed from totalAmount to Total
                        orderDate = order.OrderDate.ToString("yyyy-MM-dd HH:mm"),
                        items = order.OrderItems?.Select(oi => new
                        {
                            name = oi.FoodItemName,
                            quantity = oi.Quantity,
                            price = oi.Price
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting order details for ID: {id}");
                return Json(new { success = false, message = "Error loading order details" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            try
            {
                var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

                if (!validStatuses.Contains(status))
                {
                    TempData["ErrorMessage"] = "Invalid status.";
                    return RedirectToAction("Index");
                }

                var result = await _OrderRepository.UpdateOrderStatusAsync(id, status);

                if (result)
                {
                    TempData["SuccessMessage"] = $"Order status updated to '{status}' successfully!";

                    // Send notification to user about status change
                    var order = await _OrderRepository.GetOrderByIdAsync(id);
                    if (order != null && !string.IsNullOrEmpty(order.UserId))
                    {
                        await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveOrderUpdate",
                            $"Your order #{order.OrderNumber} status has been updated to: {status}");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update order status.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order status for ID: {id}");
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatusAjax()
        {
            try
            {
                // Read from form
                var idStr = Request.Form["id"].FirstOrDefault();
                var status = Request.Form["status"].FirstOrDefault();

                if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id))
                {
                    _logger.LogWarning($"Invalid order ID: {idStr}");
                    return Json(new { success = false, message = "Invalid order ID." });
                }

                if (string.IsNullOrEmpty(status))
                {
                    _logger.LogWarning("Status is empty");
                    return Json(new { success = false, message = "Status is required." });
                }

                _logger.LogInformation($"UpdateOrderStatusAjax: ID={id}, Status={status}");

                var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

                if (!validStatuses.Contains(status))
                {
                    _logger.LogWarning($"Invalid status: {status}");
                    return Json(new { success = false, message = "Invalid status." });
                }

                // First check if order exists
                var order = await _OrderRepository.GetOrderByIdAsync(id);
                if (order == null)
                {
                    _logger.LogWarning($"Order not found: ID={id}");
                    return Json(new { success = false, message = $"Order with ID {id} not found." });
                }

                _logger.LogInformation($"Found order: #{order.OrderNumber}, Current Status: {order.Status}");

                var result = await _OrderRepository.UpdateOrderStatusAsync(id, status);

                if (result)
                {
                    _logger.LogInformation($"Order status updated successfully: ID={id}, Status={status}");

                    // Send notification
                    if (!string.IsNullOrEmpty(order.UserId))
                    {
                        await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveOrderUpdate",
                            $"Your order #{order.OrderNumber} status has been updated to: {status}");
                    }

                    return Json(new { success = true, message = "Status updated successfully!" });
                }
                else
                {
                    _logger.LogWarning($"UpdateOrderStatusAsync returned false for order ID: {id}");
                    return Json(new { success = false, message = "Failed to update status in database." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateOrderStatusAjax");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }



    [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnnouncementNotification(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Message cannot be empty!";
                return RedirectToAction("Index");
            }

            try
            {
                var cleanMessage = message.Trim();
                _logger.LogInformation($"Admin sending message to all users: {cleanMessage}");

                // This sends to ALL connected users immediately
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", cleanMessage);

                _logger.LogInformation($"Message sent successfully: {cleanMessage}");
                TempData["SuccessMessage"] = $"Message sent to all users!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message: {ex.Message}");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    } }