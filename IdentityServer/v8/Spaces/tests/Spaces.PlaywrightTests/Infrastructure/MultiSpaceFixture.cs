using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace Spaces.PlaywrightTests.Infrastructure;

public class SpacesFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public string BaseUrl => "https://localhost:5001";

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("Spaces");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
