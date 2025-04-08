using System.Diagnostics;
using DNS_proxy.Data;
using Microsoft.EntityFrameworkCore;

namespace DNS_proxy.Utils;

public static class Utils
{
    /// <summary>
    /// Применяем миграции, если БД пустая – добавляем тестовые данные.
    /// </summary>
    public static void MigrateAndSeed()
    {
        using var db = new DnsRulesContext();
        Console.WriteLine("Применяем миграции...");
        db.Database.Migrate();
    }

    public static void EnsureFirewallRules()
    {
        string ruleName = "DNS Proxy";

        void AddRule(string protocol)
        {
            // Проверка наличия правила
            var checkInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall show rule name=\"{ruleName} ({protocol})\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var checkProc = Process.Start(checkInfo);
                string output = checkProc?.StandardOutput.ReadToEnd();
                checkProc?.WaitForExit();

                if (output != null && output.Contains("Rule Name", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"🔒 Правило брандмауэра уже существует: {ruleName} ({protocol})");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Не удалось проверить существование правила ({protocol}): {ex.Message}");
            }

            // Если не существует — добавляем
            var addInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall add rule name=\"{ruleName} ({protocol})\" " +
                            $"dir=in action=allow protocol={protocol} localport=53",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            try
            {
                var proc = Process.Start(addInfo);
                proc?.WaitForExit();
                Console.WriteLine($"✅ Разрешение {protocol} на порт 53 добавлено в брандмауэр");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Не удалось добавить правило брандмауэра ({protocol}): {ex.Message}");
            }
        }

        AddRule("UDP");
        AddRule("TCP");
    }
}
