// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.CommandLine;
using System.Net.Http.Headers;

namespace Client;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.Title = "Client";

        var token = ParseToken(args);

        var client = new HttpClient();
        Console.WriteLine($"Token: {token}");

        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:5002/identity");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("error:" + response.StatusCode);
        }

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
    }

    private static string ParseToken(string[] args)
    {
        Option<string> patOption = new("--pat")
        {
            Description = "User generated PAT from https://localhost:5001/pat"
        };

        RootCommand rootCommand = new("Sample app for System.CommandLine");
        rootCommand.Options.Add(patOption);

        var parseResult = rootCommand.Parse(args);
        if (parseResult.GetValue(patOption) is not string token
            || string.IsNullOrWhiteSpace(token))
        {
            throw new Exception("No valid string pat input to `--pat` argument.");
        }

        return token;
    }
}
