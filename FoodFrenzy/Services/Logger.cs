namespace FoodFrenzy.Services
{
    public static class Logger
    {
        private static string FilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Logs", "logger.txt");

        public static void LogExpception(Exception ex)
        {
            try
            {
                var logDir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);

                }

                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {ex.Message}\n";
                File.AppendAllText(FilePath, logMessage);

            }
            catch
            {
                Console.WriteLine("There is error to Create the FIle");

            }



        }
        public static void LogMessage(string message)
        {
            try
            {
                var logDir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);

                }

                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}\n";
                File.AppendAllText(FilePath, logMessage);

            }
            catch
            {
                Console.WriteLine("There is error to Create the FIle");

            }



        }
    }
}