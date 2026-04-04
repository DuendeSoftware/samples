// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.Extensions.Hosting;

public static class ServiceDiscovery
{
    public static Uri ResolveService(string serviceName, string appName = "https")
    {
        var host = serviceName;

        // Compose the environment variable key
        var envVarKey = $"services__{host}__{appName}__0";

        // Try to get the value from environment variables
        var value = Environment.GetEnvironmentVariable(envVarKey);

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Service endpoint for '{serviceName}' not found in environment variable '{envVarKey}'.");
        }

        return new Uri(value);
    }
}
