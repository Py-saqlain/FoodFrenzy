using System.Collections.Generic;
using FoodFrenzy.Models;

namespace FoodFrenzy.Repositories
{
    public interface IFoodItemRepository
    {
        IEnumerable<FoodItem> GetAllFoodItems();
        IEnumerable<FoodItem> GetTopThreeFoodItems();
        FoodItem GetFoodItemById(int id);
        bool AddFoodItem(FoodItem foodItem);
        bool UpdateFoodItem(FoodItem foodItem);
        bool DeleteFoodItem(int id);
        bool FoodItemHasOrders(int foodItemId);
    }
}