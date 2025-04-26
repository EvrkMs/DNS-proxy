using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DnsProxy.Pages.Logs;
public class IndexModel : PageModel
{
    public string LogText { get; private set; } = "";

    public void OnGet()
    {
        var latest = Directory.GetFiles("logs", "log-*.txt")
                              .OrderByDescending(f => f)
                              .FirstOrDefault();
        LogText = latest is null ? "Логов нет" : System.IO.File.ReadAllText(latest);
    }
}
