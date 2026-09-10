using Microsoft.Playwright;
using Spaces.PlaywrightTests.Infrastructure;
using Xunit;

namespace Spaces.PlaywrightTests.Tests;

public class ClientCredentialsTests(SpacesFixture fixture)
{
    [Fact]
    public async Task Can_get_token_for_default_space()
    {
        await using var traced = await BrowserContextFactory.CreateAsync();
        var page = await traced.Context.NewPageAsync();

        await page.GotoAsync(fixture.BaseUrl);
        await page.CheckAsync("input[name=SelectedClient][value=space]");
        await page.ClickAsync("button:has-text('Request Token')");
        await page.WaitForLoadStateAsync();

        await Assertions.Expect(page.Locator(".token-output")).ToBeVisibleAsync();
        var payload = await page.Locator(".token-json").Last.TextContentAsync();
        Assert.Contains("\"client_id\": \"default-client\"", payload);
        Assert.Contains("\"client_space\": \"default\"", payload);
    }

    [Fact]
    public async Task Can_get_token_for_space1()
    {
        await using var traced = await BrowserContextFactory.CreateAsync();
        var page = await traced.Context.NewPageAsync();

        // Navigate to space1 via link from home
        await page.GotoAsync(fixture.BaseUrl);
        await page.ClickAsync("a:has-text('space1.dev.localhost')");
        await page.WaitForLoadStateAsync();

        // Request token using the space client
        await page.CheckAsync("input[name=SelectedClient][value=space]");
        await page.ClickAsync("button:has-text('Request Token')");
        await page.WaitForLoadStateAsync();

        await Assertions.Expect(page.Locator(".token-output")).ToBeVisibleAsync();
        var payload = await page.Locator(".token-json").Last.TextContentAsync();
        Assert.Contains("\"client_id\": \"space1-client\"", payload);
        Assert.Contains("\"client_space\": \"space1\"", payload);
    }

    [Fact]
    public async Task Can_get_token_for_space3()
    {
        await using var traced = await BrowserContextFactory.CreateAsync();
        var page = await traced.Context.NewPageAsync();

        // Navigate to space3 via link from home
        await page.GotoAsync(fixture.BaseUrl);
        await page.ClickAsync("a:has-text('/space3')");
        await page.WaitForLoadStateAsync();

        // Request token using the space client
        await page.CheckAsync("input[name=SelectedClient][value=space]");
        await page.ClickAsync("button:has-text('Request Token')");
        await page.WaitForLoadStateAsync();

        await Assertions.Expect(page.Locator(".token-output")).ToBeVisibleAsync();
        var payload = await page.Locator(".token-json").Last.TextContentAsync();
        Assert.Contains("\"client_id\": \"space3-client\"", payload);
        Assert.Contains("\"client_space\": \"space3\"", payload);
    }
}
