// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.AspNetCore.AccessTokenManagement;
using IdentityModel.Client;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorServer.Data
{
    public class WeatherForecastService
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IUserAccessTokenManagementService _userAccessTokenManagementService;
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherForecastService(
            AuthenticationStateProvider authenticationStateProvider,
            IUserAccessTokenManagementService userAccessTokenManagementService,
            IHttpClientFactory httpClientFactory)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _userAccessTokenManagementService = userAccessTokenManagementService;
            _httpClientFactory = httpClientFactory;
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public async Task<Weather> GetForecastAsync(DateTime startDate)
        {
            var weather = new Weather();

            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();

            if (!state.User.Identity.IsAuthenticated)
            {
                weather.User = "anonymous";
            }
            else
            {
                var token = await _userAccessTokenManagementService.GetUserAccessTokenAsync(state.User);
                var client = _httpClientFactory.CreateClient("api_client");
                client.SetBearerToken(token);

                var userName = await client.GetStringAsync("identity");

                weather.User = userName;
            }

            var rng = new Random();
            weather.Forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }).ToArray();

            return weather;
        }
    }
}
