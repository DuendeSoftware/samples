// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TokenExchange.Api;

[AllowAnonymous]
public class GreetingsController : ControllerBase
{
    [HttpGet("{**catch-all}")]
    public IActionResult Get()
    {
        string message;
        var sub = User.FindFirst("sub");

        if (!User.Identity.IsAuthenticated)
        {
            message = "Hello, anonymous caller";
        }
        else if (sub != null)
        {
            var userName = User.FindFirst("name");
            message = $"Hello user, {userName.Value}";
        }
        else
        {
            var client = User.FindFirst("client_id");
            message = $"Hello client, {client.Value}";
        }

        var response = new
        {
            path = Request.Path.Value,
            message = message,
            time = DateTime.UtcNow.ToString(),
            headers = Request.Headers.ToDictionary(x => x.Key, x => string.Join(',', x))
        };

        return Ok(response);
    }
}
