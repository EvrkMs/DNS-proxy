using DnsProxy.Data;
using DnsProxy.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// хранить в каталоге /app/data/proxy.db
// 1. Определяем папку ProgramData (CommonApplicationData)
var commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
// 2. В нашей подпапке (например, "DnsProxy") создаём каталог, если его нет
var appDataDir = Path.Combine(commonData, "DnsProxy");
if (!Directory.Exists(appDataDir))
{
    Directory.CreateDirectory(appDataDir);
}
var dbPath = Path.Combine(appDataDir, "proxy.db");

// Регистрируем контекст с этим путём
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddMemoryCache();

// Program.cs  ─ после builder.Host…
builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Information()               // всё, что ниже, игнорируем
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.File("logs/errors-.txt",
                  rollingInterval: RollingInterval.Day,
                  restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
                  retainedFileCountLimit: 7)
);

builder.Services.AddHttpClient();

builder.Services.AddScoped<IDnsConfigService, DnsConfigService>();
builder.Services.AddScoped<IRuleService, RuleService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IResolverService, ResolverService>();

builder.Services.AddSingleton<DnsProxyServer>();
builder.Services.AddHostedService<DnsBackground>();

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();         // 1
app.UseRouting();             // 2
app.UseAuthorization();       // 2 (останется no-op, пока auth не добавишь)

app.MapRazorPages();
app.MapPost("/admin/flush", (ICacheService c) =>
{
    if (c is MemoryCacheService m) m.Clear();
    Log.Logger.Information("Cache flushed");
    return Results.NoContent();
});

Seeder.Seed(app.Services);
app.Run();

class DnsBackground(DnsProxyServer srv) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        srv.Start();
        return Task.CompletedTask;
    }
}
