namespace Services
{
    public static class Logger
    {
        public static void LogWithTimestamp(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }
    }
}
