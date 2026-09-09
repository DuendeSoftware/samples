// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Test;

namespace Storage;

internal static class SampleUsers
{
    internal static List<TestUser> Users { get; } =
    [
        new()
        {
            SubjectId = "1",
            Username = "alice",
            Password = "alice",
            Claims =
            {
                new Claim(JwtClaimTypes.Name, "Alice Smith"),
                new Claim(JwtClaimTypes.Email, "alice@example.com"),
                new Claim(JwtClaimTypes.Role, "admin")
            }
        }
    ];
}
