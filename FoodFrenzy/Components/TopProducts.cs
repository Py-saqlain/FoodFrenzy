using Microsoft.AspNetCore.Mvc;
using FoodFrenzy.Models;

namespace FoodFrenzy.Components
{
    public class TopProducts:ViewComponent
    {
      
        public IViewComponentResult Invoke(List<FoodItem> items)
        {


            return View("default", items);

        }
    }

}
