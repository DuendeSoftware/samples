using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace Spaces.PlaywrightTests.Infrastructure;

public static class BrowserContextFactory
{
    public static async Task<TracedBrowserContext> CreateAsync([CallerMemberName] string caller = "")
    {
        var playwright = await Playwright.CreateAsync();
        var headless = Environment.GetEnvironmentVariable("CI") is not null;
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless
        });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        return new TracedBrowserContext(playwright, browser, context, caller);
    }
}

public sealed class TracedBrowserContext(
    IPlaywright playwright,
    IBrowser browser,
    IBrowserContext context,
    string caller)
    : IAsyncDisposable
{
    public IBrowserContext Context { get; } = context;

    public async ValueTask DisposeAsync()
    {
        var dir = Path.Combine("artifacts", "playwright-traces");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{caller}-{Guid.NewGuid():N}.zip");
        await Context.Tracing.StopAsync(new TracingStopOptions { Path = path });
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
