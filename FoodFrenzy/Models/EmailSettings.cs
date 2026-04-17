namespace FoodFrenzy.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public bool UseDefaultCredentials { get; set; }
    }
}