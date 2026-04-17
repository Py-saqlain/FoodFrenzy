using Microsoft.AspNetCore.Mvc;
using FoodFrenzy.Models;
using FoodFrenzy.Repositories;
using System.Linq;

namespace FoodFrenzy.Controllers
{
    public class ExploreController : Controller
    {
        private readonly IFoodItemRepository _foodRepository;

        public ExploreController(IFoodItemRepository foodRepository)
        {
            _foodRepository = foodRepository;
        }

        public IActionResult Index(string search = "", string category = "", string sortBy = "name", string availability = "all")
        {
            var foodItems = _foodRepository.GetAllFoodItems().AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                foodItems = foodItems.Where(f =>
                    f.Name.ToLower().Contains(search) ||
                    f.Description.ToLower().Contains(search) ||
                    f.Category.ToLower().Contains(search));
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(category) && category != "all")
            {
                foodItems = foodItems.Where(f => f.Category.ToLower() == category.ToLower());
            }

            // Apply availability filter
            if (availability != "all")
            {
                bool isAvailable = availability == "available";
                foodItems = foodItems.Where(f => f.IsAvailable == isAvailable);
            }

            // Apply sorting
            foodItems = sortBy.ToLower() switch
            {
                "price_low" => foodItems.OrderBy(f => f.Price),
                "price_high" => foodItems.OrderByDescending(f => f.Price),
                "rating" => foodItems.OrderByDescending(f => f.Rating),
                "name" => foodItems.OrderBy(f => f.Name),
                _ => foodItems.OrderBy(f => f.Name)
            };

            // Get unique categories for filter dropdown
            var categories = _foodRepository.GetAllFoodItems()
                .Select(f => f.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedSort = sortBy;
            ViewBag.SelectedAvailability = availability;
            ViewBag.Categories = categories;

            return View(foodItems.ToList());
        }

        [HttpGet]
        public IActionResult GetCategories()
        {
            var categories = _foodRepository.GetAllFoodItems()
                .Select(f => f.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Json(categories);
        }
    }
}