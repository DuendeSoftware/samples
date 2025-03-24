// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

public class MaxAgeHandler : AuthorizationHandler<MaxAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx,
        MaxAgeRequirement requirement)
    {
        var authTimeClaim = ctx.User.FindFirst("auth_time")?.Value;
        if (authTimeClaim == null)
        {
            return Task.CompletedTask;
        }

        var authTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(authTimeClaim));

        var timeSinceAuth = DateTime.UtcNow - authTime;

        if (timeSinceAuth < requirement.MaxAge)
        {
            ctx.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
