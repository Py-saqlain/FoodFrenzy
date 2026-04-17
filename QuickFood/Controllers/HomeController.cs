using Microsoft.AspNetCore.Mvc;
using FoodFrenzy.Models.Interfaces;
using FoodFrenzy.Repositories;

namespace FoodFrenzy.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFoodItemRepository _foodRepository;

        public HomeController(IFoodItemRepository foodRepository)
        {
            _foodRepository = foodRepository;
        }
        public IActionResult Index()
        {
            var products=_foodRepository.GetTopThreeFoodItems();
            return View(products);
        }


    }
}
