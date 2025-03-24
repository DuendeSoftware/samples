// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("step-up")]
public class StepUp : ControllerBase
{
    [HttpGet]
    [Route("max-age")]
    [Authorize("MaxAgeOneMinute")]
    public IEnumerable<string> MaxAge()
    {
        yield return ShowAge();
    }

    [HttpGet]
    [Route("mfa")]
    [Authorize("MfaRequired")]
    public IEnumerable<string> MfaRequired()
    {
        yield return ShowAmrValues();
    }


    [HttpGet]
    [Route("both")]
    [Authorize("RecentMfa")]
    public IEnumerable<string> Both()
    {
        yield return ShowAge();
        yield return ShowAmrValues();
    }

    [HttpGet]
    [Route("neither")]
    [Authorize]
    public IEnumerable<string> Neither()
    {
        yield return $"Request is authenticated";
    }

    private string ShowAge()
    {
        var authTime = long.Parse(User.FindFirst("auth_time")!.Value);
        var age = DateTime.UtcNow - DateTimeOffset.FromUnixTimeSeconds(authTime);
        return $"Authenticated {age.TotalSeconds} seconds ago";
    }

    private string ShowAmrValues()
    {
        var amrValues = string.Join(',', User.FindAll("amr").Select(c => c.Value));
        return $"Authenticated using {amrValues}";
    }
}
