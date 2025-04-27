using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DnsProxy.Pages.Logs;

public class IndexModel : PageModel
{
    public string LogText { get; private set; } = "";

    public void OnGet() => LoadLastFile();

    /* ---------- POST /Logs?handler=Clear ---------- */
    public IActionResult OnPostClear()
    {
        foreach (var f in Directory.GetFiles("logs", "errors-*.txt"))
            System.IO.File.Delete(f);

        LoadLastFile();                     // показать обновлённый текст
        return Page();                      // остаёмся на той же странице
    }

    /* ---------- helpers ---------- */
    private void LoadLastFile()
    {
        var latest = Directory.GetFiles("logs", "errors-*.txt")
                              .OrderByDescending(f => f)
                              .FirstOrDefault();

        LogText = latest is null
            ? "Логов нет"
            : System.IO.File.ReadAllText(latest);
    }
}
