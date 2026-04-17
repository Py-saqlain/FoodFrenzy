namespace FoodFrenzy.Models.Interfaces
{
    public interface ICartRepository
    {
        Task<List<CartItem>> GetCartAsync(string userId, ISession session);
        Task AddToCartAsync(string userId, int foodItemId, string name, decimal price, string imageUrl, ISession session);
        Task UpdateCartItemAsync(string userId, int cartItemId, int quantity, ISession session);
        Task RemoveCartItemAsync(string userId, int cartItemId, ISession session);
        Task ClearCartAsync(string userId, ISession session);
        Task MergeCartsAsync(string userId, ISession session);
    }
}
