using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FoodFrenzy.Controllers
{
    [Authorize]
    public class TrackingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}