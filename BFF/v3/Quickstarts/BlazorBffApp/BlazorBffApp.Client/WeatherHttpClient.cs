﻿using System.Net.Http.Json;
using System.Text.Json;

public class WeatherHttpClient(HttpClient client) : IWeatherClient
{
    public async Task<WeatherForecast[]> GetWeatherForecasts() => await client.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast")
                                                                  ?? throw new JsonException("Failed to deserialize");
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public interface IWeatherClient
{
    Task<WeatherForecast[]> GetWeatherForecasts();
}