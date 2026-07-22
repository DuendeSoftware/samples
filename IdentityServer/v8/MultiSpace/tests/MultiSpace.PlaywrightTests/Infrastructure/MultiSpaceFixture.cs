using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace MultiSpace.PlaywrightTests.Infrastructure;

public class MultiSpaceFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public string BaseUrl => "https://localhost:5000";

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("multispace");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
