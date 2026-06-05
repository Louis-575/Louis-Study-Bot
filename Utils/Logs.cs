namespace LouisStudyBot.Utils;

public static class Logs
{
    private static readonly object LockObject = new();
    private static string _logPath = string.Empty;

    public static void Initialize(string logPath)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        _logPath = logPath
            .Replace("[year]", now.Year.ToString("0000"))
            .Replace("[month]", now.Month.ToString("00"))
            .Replace("[day]", now.Day.ToString("00"));

        string? directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static void Info(string message) => Write("Info", message, ConsoleColor.Cyan);

    public static void Warning(string message) => Write("Warning", message, ConsoleColor.Yellow);

    public static void Error(string message) => Write("Error", message, ConsoleColor.Red);

    public static void Debug(string message)
    {
        if (EnvConfig.Get("LOG_LEVEL", "Info").Equals("Debug", StringComparison.OrdinalIgnoreCase))
        {
            Write("Debug", message, ConsoleColor.Gray);
        }
    }

    public static void Shutdown()
    {
    }

    private static void Write(string level, string message, ConsoleColor color)
    {
        string line = $"{DateTimeOffset.Now:HH:mm:ss.fff} [{level}] {message}";
        lock (LockObject)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ResetColor();

            if (!string.IsNullOrWhiteSpace(_logPath))
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
    }
}
