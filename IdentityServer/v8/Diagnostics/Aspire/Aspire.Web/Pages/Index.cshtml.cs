using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Aspire.Web.Pages;

public class IndexModel(WeatherApiClient weatherClient, ILogger<IndexModel> logger) : PageModel
{
    public WeatherForecast[] Forecasts { get; private set;  } = default!;

    public async Task OnGet()
    {
        logger.LogDebug("Getting forecasts...");
        Forecasts = await weatherClient.GetWeatherAsync();
    }
}
