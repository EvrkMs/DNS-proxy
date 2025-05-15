using System.Net;
using DnsProxy.Data;
using DnsProxy.Services;
using DnsProxy.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

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
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Error)     // только Error+
    .WriteTo.File("logs/errors-.txt",
                  rollingInterval: RollingInterval.Day,
                  restrictedToMinimumLevel: LogEventLevel.Error,
                  retainedFileCountLimit: 7));

builder.Services.AddScoped<IDnsConfigService, DnsConfigService>();
builder.Services.AddScoped<IRuleService, RuleService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IResolverService, ResolverService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<QueryMethot>();

builder.Services.AddSingleton<IHttpClientPerServerService, HttpClientPerServerService>();
builder.Services.AddSingleton<ICacheService, SimpleDnsCacheService>();
builder.Services.AddSingleton<DnsProxyServer>();
builder.Services.AddHostedService<DnsBackground>();

builder.Services.AddRazorPages();

var app = builder.Build();
if (app.Environment.IsDevelopment())          // подробный стек в браузере
    app.UseDeveloperExceptionPage();

app.UseStaticFiles();         // 1
app.UseRouting();             // 2
app.UseAuthorization();       // 2 (останется no-op, пока auth не добавишь)

app.MapRazorPages();
app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    // Разрешаем доступ к Razor Pages только с localhost
    if (path.StartsWithSegments("/admin") || path.StartsWithSegments("/"))
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        if (!IPAddress.IsLoopback(remoteIp))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("403 - Forbidden");
            return;
        }
    }

    await next();
});

app.MapPost("/admin/flush", (ICacheService c) =>
{
    c.Clear();
    Log.Logger.Information("Cache flushed");
    return Results.NoContent();
});

app.MapPost("/admin/flushstat", async (IServiceProvider services) =>
{
    using var scope = services.CreateScope();
    var stat = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
    await stat.ClearStats();
    
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
