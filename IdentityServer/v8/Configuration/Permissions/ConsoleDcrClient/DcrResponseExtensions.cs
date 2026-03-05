// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Duende.IdentityModel.Client;

namespace ConsoleDcrClient;

public static class DcrResponseExtensions
{
    public static void Show(this DynamicClientRegistrationResponse response)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            response.ClientId,
            response.ClientSecret
        }, new JsonSerializerOptions { WriteIndented = true }));

    }
}
