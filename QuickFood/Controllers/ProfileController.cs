using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FoodFrenzy.Models;
using FoodFrenzy.Models.Interfaces;
using FoodFrenzy.Models.Repositories;
using static NuGet.Packaging.PackagingConstants;

namespace FoodFrenzy.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileRepository _profileRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOrderRepository _orderRepository;

        public ProfileController(IProfileRepository profileRepository, UserManager<ApplicationUser> userManager , IOrderRepository orderRepository)
        {
            _profileRepository = profileRepository;
            _userManager = userManager;
            _orderRepository = orderRepository;
        }

        public async Task<IActionResult> IndexAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _profileRepository.GetUserProfile(userId);
            var orderHistory = _profileRepository.GetOrderHistory(userId);

            var orders =  await _orderRepository.GetUserOrdersAsync(userId);
            ViewBag.Orders = orders;
            ViewBag.OrderHistory = orderHistory;
             
            return View(user);
           
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber, string email)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Validate required fields
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email))
                {
                    TempData["ErrorMessage"] = "First name, last name, and email are required.";
                    return RedirectToAction("Index");
                }

                var existingUser = _profileRepository.GetUserProfile(userId);
                if (existingUser == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Update user properties
                existingUser.FullName = $"{firstName} {lastName}".Trim();
                existingUser.PhoneNumber = phoneNumber;
                existingUser.Email = email;

                var result = _profileRepository.UpdateProfile(existingUser);
                if (result)
                {
                    // Also update the user in Identity system
                    var identityUser = await _userManager.FindByIdAsync(userId);
                    if (identityUser != null)
                    {
                        identityUser.Email = email;
                        identityUser.UserName = email;
                        identityUser.PhoneNumber = phoneNumber;
                        identityUser.FullName = existingUser.FullName;
                        await _userManager.UpdateAsync(identityUser);
                    }

                    TempData["SuccessMessage"] = "Profile updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error updating profile. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
                {
                    TempData["ErrorMessage"] = "New password and confirm password do not match.";
                    return RedirectToAction("Index");
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Password changed successfully!";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Error changing password: {errors}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAddress(string address)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                if (string.IsNullOrEmpty(address))
                {
                    TempData["ErrorMessage"] = "Address is required.";
                    return RedirectToAction("Index");
                }

                var result = _profileRepository.UpdateAddress(userId, address);

                if (result)
                {
                    TempData["SuccessMessage"] = "Address updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error updating address.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}