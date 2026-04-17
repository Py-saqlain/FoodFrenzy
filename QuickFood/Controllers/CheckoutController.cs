using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodFrenzy.Models;
using FoodFrenzy.Models.Interfaces;
using FoodFrenzy.Models.Repositories;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoodFrenzy.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ICartRepository _cartRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ICartRepository cartRepository,
            IOrderRepository orderRepository,
            ILogger<CheckoutController> logger)
        {
            _cartRepository = cartRepository;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await _cartRepository.GetCartAsync(userId, HttpContext.Session);

                if (cart == null || !cart.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty. Please add items before checkout.";
                    return RedirectToAction("Index", "Cart");
                }

                // Calculate totals
                decimal subtotal = cart.Sum(item => item.Price * item.Quantity);
                decimal deliveryFee = subtotal > 0 ? 250m : 0;
                decimal serviceFee = subtotal > 0 ? 100m : 0;
                decimal tax = subtotal * 0.08m;
                decimal total = subtotal + deliveryFee + serviceFee + tax;

                ViewBag.Cart = cart;
                ViewBag.Subtotal = subtotal;
                ViewBag.DeliveryFee = deliveryFee;
                ViewBag.ServiceFee = serviceFee;
                ViewBag.Tax = tax;
                ViewBag.Total = total;
                ViewBag.ItemCount = cart.Sum(item => item.Quantity);

                // Get user details if available
                var user = await GetCurrentUserAsync();
                if (user != null)
                {
                    ViewBag.UserEmail = user.Email;
                    ViewBag.UserFullName = user.FullName;
                    ViewBag.UserPhone = user.PhoneNumber;
                    ViewBag.UserAddress = user.Address;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["ErrorMessage"] = "An error occurred while loading checkout.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder([FromForm] OrderViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please fill in all required fields.";
                    return RedirectToAction("Index");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await _cartRepository.GetCartAsync(userId, HttpContext.Session);

                if (cart == null || !cart.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty. Please add items before checkout.";
                    return RedirectToAction("Index", "Cart");
                }

                // Calculate totals
                decimal subtotal = cart.Sum(item => item.Price * item.Quantity);
                decimal deliveryFee = subtotal > 0 ? 250m : 0;
                decimal serviceFee = subtotal > 0 ? 100m : 0;
                decimal tax = subtotal * 0.08m;
                decimal total = subtotal + deliveryFee + serviceFee + tax;

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    Subtotal = subtotal,
                    DeliveryFee = deliveryFee,
                    ServiceFee = serviceFee,
                    Tax = tax,
                    Total = total,
                    CustomerName = model.CustomerName,
                    CustomerEmail = model.CustomerEmail,
                    CustomerPhone = model.CustomerPhone,
                    DeliveryAddress = model.DeliveryAddress,
                    City = model.City,
                    ZipCode = model.ZipCode,
                    DeliveryInstructions = model.DeliveryInstructions,
                    PaymentMethod = model.PaymentMethod,
                    //DeliveryMethod = model.DeliveryMethod,
                    Status = "Confirmed"
                };

                // Save order
                var createdOrder = await _orderRepository.CreateOrderAsync(order, cart);

                // Clear cart
                await _cartRepository.ClearCartAsync(userId, HttpContext.Session);

                // Redirect to tracking page
                return RedirectToAction("Tracking", new { trackingNumber = createdOrder.TrackingNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                TempData["ErrorMessage"] = "An error occurred while placing your order. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Tracking(string trackingNumber)
        {
            if (string.IsNullOrEmpty(trackingNumber))
            {
                TempData["ErrorMessage"] = "Tracking number is required.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var order = await _orderRepository.GetOrderByTrackingNumberAsync(trackingNumber);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if user owns this order
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (order.UserId != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to view this order.";
                    return RedirectToAction("Index", "Home");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tracking page");
                TempData["ErrorMessage"] = "An error occurred while loading order details.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var orders = await _orderRepository.GetUserOrdersAsync(userId);

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user orders");
                TempData["ErrorMessage"] = "An error occurred while loading your orders.";
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            // This would typically come from UserManager
            // For now, return null or implement as needed
            return null;
        }
    }

    
}