using System.Text;

namespace DNS_proxy.Utils;

public static class Logger
{
    public static Action<string> OnLog = _ => { };

    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DNS Proxy", "log.txt"
    );

    private static readonly object _lock = new();

    static Logger()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
    }

    public static void Log(string message)
    {
        string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        OnLog?.Invoke(logLine);
        //WriteToFile(logLine);
    }

    private static void WriteToFile(string line)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Logger] Не удалось записать в лог: {ex.Message}");
        }
    }

    public static string GetLogPath() => LogFilePath;
}
