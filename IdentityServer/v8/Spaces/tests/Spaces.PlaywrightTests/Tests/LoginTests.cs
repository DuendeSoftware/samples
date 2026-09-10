using Spaces.PlaywrightTests.Infrastructure;
using Xunit;

namespace Spaces.PlaywrightTests.Tests;

public class LoginTests(SpacesFixture fixture)
{
    [Fact]
    public async Task Can_login_to_default_space()
    {
        await using var traced = await BrowserContextFactory.CreateAsync();
        var page = await traced.Context.NewPageAsync();

        await page.GotoAsync(fixture.BaseUrl);
        await page.ClickAsync("a:has-text('Sign in')");
        await page.FillAsync("[name=Username]", "default-user");
        await page.FillAsync("[name=Password]", "Pa$$Word123");
        await page.ClickAsync("button[type=submit]");

        await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
    }

    [Fact]
    public async Task Can_login_to_space1_via_hostname()
    {
        await using var traced = await BrowserContextFactory.CreateAsync();
        var page = await traced.Context.NewPageAsync();

        await page.GotoAsync(fixture.BaseUrl);
        await page.ClickAsync("a:has-text('space1.dev.localhost')");
        await page.ClickAsync("a:has-text('Sign in')");
        await page.FillAsync("[name=Username]", "space1-user");
        await page.FillAsync("[name=Password]", "Pa$$Word123");
        await page.ClickAsync("button[type=submit]");

        await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
    }
}
