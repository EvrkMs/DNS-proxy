using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS_proxy.Utils;

public static class Logger
{
    public static Action<string> OnLog = _ => { };

    public static void Log(string message)
    {
        OnLog?.Invoke($"[{DateTime.Now:T}] {message}");
    }
}
