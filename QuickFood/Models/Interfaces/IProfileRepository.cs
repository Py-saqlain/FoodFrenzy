namespace FoodFrenzy.Models.Interfaces
{
    public interface IProfileRepository
    {
        ApplicationUser GetUserProfile(string userId);
        bool UpdateProfile(ApplicationUser user);
        bool ChangePassword(string userId, string currentPassword, string newPassword);
        List<OrderHistory> GetOrderHistory(string userId);
        bool UpdateAddress(string userId, string address);
        IEnumerable<ApplicationUser> GetAllUser();

        Task<bool> DeleteUserAsync(string userId);
    }
}