// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

public class MaxAgeRequirement : IAuthorizationRequirement
{
    public MaxAgeRequirement(TimeSpan maxAge)
    {
        MaxAge = maxAge;
    }

    public TimeSpan MaxAge { get; }
}
