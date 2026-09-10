// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Spaces.Seed;

/// <summary>
/// Single source of truth for demo credentials seeded per space.
/// Used by SeedData to create users and by page models to display credentials.
/// </summary>
public static class DemoCredentials
{
    /// <summary>Demo password shared across all seeded users.</summary>
    public const string Password = "Pa$$Word123";

    /// <summary>
    /// Returns the demo username for a given space, or null if no demo user is seeded for that space.
    /// </summary>
    public static string? GetUsernameForSpace(string spaceName)
    {
        // Non-default space IDs encode the space name
        var normalizedSpaceName = spaceName.ToLowerInvariant();
        return normalizedSpaceName switch
        {
            "default" => "default-user",
            "space1" => "space1-user",
            "space2" => "space2-user",
            _ => null
        };
    }
}
