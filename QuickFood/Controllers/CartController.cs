using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodFrenzy.Models;
using FoodFrenzy.Models.Interfaces;
using System.Security.Claims;

namespace FoodFrenzy.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepo;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartRepository cartRepo, ILogger<CartController> logger)
        {
            _cartRepo = cartRepo;
            _logger = logger;
        }

        // GET: /Cart/Checkout - Proceed to checkout (requires login)
        [Authorize]
        public IActionResult Checkout()
        {
            // Redirect to the CheckoutController's Index action
            return RedirectToAction("Index", "Checkout");
        }

        // GET: /Cart - Show cart page
        public async Task<IActionResult> Index()
        {
            try
            {
                string userId = null;

                // Check if user is authenticated
                if (User.Identity.IsAuthenticated)
                {
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    ViewBag.IsAuthenticated = true;
                }
                else
                {
                    // For non-authenticated users, use session cart
                    ViewBag.IsAuthenticated = false;
                }

                // Get cart items (will handle both authenticated and session carts)
                var cart = await _cartRepo.GetCartAsync(userId, HttpContext.Session);

                // Calculate totals
                decimal subtotal = cart.Sum(item => item.Price * item.Quantity);
                decimal deliveryFee = subtotal > 0 ? 250m : 0;
                decimal serviceFee = subtotal > 0 ? 100m : 0;
                decimal tax = subtotal * 0.08m;
                decimal total = subtotal + deliveryFee + serviceFee + tax;

                ViewBag.Subtotal = subtotal.ToString("0.00");
                ViewBag.DeliveryFee = deliveryFee.ToString("0.00");
                ViewBag.ServiceFee = serviceFee.ToString("0.00");
                ViewBag.Tax = tax.ToString("0.00");
                ViewBag.Total = total.ToString("0.00");
                ViewBag.ItemCount = cart.Sum(item => item.Quantity);

                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart page");
                TempData["ErrorMessage"] = "An error occurred while loading your cart.";
                return View(new List<CartItem>());
            }
        }

        // POST: /Cart/Add - Add item to cart
        [HttpPost]
        public async Task<IActionResult> Add([FromForm] CartItemRequest request)
        {
            try
            {
                _logger.LogInformation("Add to cart called with: FoodItemId={FoodItemId}, Name={Name}",
                    request.FoodItemId, request.Name);

                string userId = User.Identity.IsAuthenticated ?
                    User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

                await _cartRepo.AddToCartAsync(userId, request.FoodItemId, request.Name,
                    request.Price, request.ImageUrl, HttpContext.Session);

                // Get updated cart count
                var cart = await _cartRepo.GetCartAsync(userId, HttpContext.Session);
                var itemCount = cart.Sum(item => item.Quantity);

                // Always return JSON for consistency
                return Json(new
                {
                    success = true,
                    message = $"{request.Name} added to cart!",
                    itemCount = itemCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");

                return Json(new
                {
                    success = false,
                    message = "Error adding item to cart: " + ex.Message
                });
            }
        }

        // POST: /Cart/Update - Update item quantity
        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            try
            {
                string userId = User.Identity.IsAuthenticated ?
                    User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

                await _cartRepo.UpdateCartItemAsync(userId, cartItemId, quantity, HttpContext.Session);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item");
                TempData["ErrorMessage"] = "Error updating item quantity";
                return RedirectToAction("Index");
            }
        }

        // POST: /Cart/Remove - Remove item from cart
        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            try
            {
                string userId = User.Identity.IsAuthenticated ?
                    User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

                await _cartRepo.RemoveCartItemAsync(userId, cartItemId, HttpContext.Session);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item");
                TempData["ErrorMessage"] = "Error removing item from cart";
                return RedirectToAction("Index");
            }
        }

        // POST: /Cart/Clear - Clear all items from cart
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            try
            {
                string userId = User.Identity.IsAuthenticated ?
                    User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

                await _cartRepo.ClearCartAsync(userId, HttpContext.Session);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["ErrorMessage"] = "Error clearing cart";
                return RedirectToAction("Index");
            }
        }

        // AJAX endpoint to get cart count
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                string userId = User.Identity.IsAuthenticated ?
                    User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

                var cart = await _cartRepo.GetCartAsync(userId, HttpContext.Session);
                var itemCount = cart.Sum(item => item.Quantity);

                return Json(new { count = itemCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { count = 0 });
            }
        }
    }

    // Simple request model for cart items
    public class CartItemRequest
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}