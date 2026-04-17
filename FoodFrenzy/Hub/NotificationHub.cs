using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FoodFrenzy.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        // This method already exists - for sending to ALL users
        public async Task SendNotification(string message)
        {
            _logger.LogInformation($"📢 Sending notification to ALL users: {message}");
            await Clients.All.SendAsync("ReceiveNotification", message);
            _logger.LogInformation($"✅ Notification sent: {message}");
        }
    }
}