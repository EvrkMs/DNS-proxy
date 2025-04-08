using DNS_proxy.Service;
using DNS_proxy.UI;
using static DNS_proxy.Utils.Utils;

namespace DNS_proxy;

public class AppContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly MainForm _mainForm;
    private readonly CustomDnsServer _server = DnsServerInstance.Server;

    public AppContext()
    {
        _server.OnLog += AppendLogToUi;
        // Запускаем миграции и сервер
        Console.WriteLine("DNS-сервер запускается...");
        MigrateAndSeed();

        // Создаем форму, но не показываем
        _mainForm = new MainForm(_server);
        _mainForm.FormClosed += (_, _) => ExitThread();

        // Создаем иконку в трее
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application, // можно заменить на свою
            Visible = true,
            Text = "DNS Proxy",
            ContextMenuStrip = new ContextMenuStrip()
            {
                Items =
            {
                new ToolStripMenuItem("Открыть интерфейс", null, (_,_) => _mainForm.Show()),
                new ToolStripMenuItem("Выход", null, (_,_) => Exit())
            }
            }
        };

        _trayIcon.DoubleClick += (_, _) => _mainForm.Show();
    }
    private void AppendLogToUi(string message)
    {
        if (_mainForm?.InvokeRequired == true)
        {
            _mainForm.Invoke(new Action<string>(AppendLogToUi), message);
            return;
        }

        _mainForm?.AppendLog(message);
    }
    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _server?.Dispose();
        base.ExitThreadCore();
    }

    private void Exit()
    {
        _mainForm?.Close();
        _trayIcon.Visible = false;
        ExitThread();
    }
}
