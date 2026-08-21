// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.CommandLine;

using Duende.IdentityModel.Client;
namespace Client;

class Program
{
    private static DiscoveryCache Cache;

    static async Task Main(string[] args)
    {
        Console.Title = "Console Resources and Scopes Client";
        var resource = ParseResource(args);

        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync();

        Cache = new DiscoveryCache("https://localhost:5001");

        Console.WriteLine($"Resources and Scopes option: {resource}");

        switch (resource)
        {
            case "help":
                OutputHelpContent();
                break;

            case "a":
                await RequestToken("scope3 scope4");
                break;

            case "b":
                await RequestToken("resource1.scope1");
                break;

            case "c":
                await RequestToken("resource1.scope1 resource1.scope2");
                break;

            case "d":
                await RequestToken("resource1.scope1 scope3");
                break;

            case "e":
                await RequestToken("resource1.scope1 resource2.scope1");
                break;

            case "f":
                await RequestToken("shared.scope");
                break;

            case "g":
                await RequestToken("resource1.scope1 shared.scope");
                break;

            case "h":
                await RequestToken("transaction:123");
                break;

            case "i":
                await RequestToken("");
                break;

            case "j":
                await RequestToken("", "urn:resource1");
                break;

            case "k":
                await RequestToken("", "urn:resource3");
                break;

            case "l":
                await RequestToken("resource3.scope1");
                break;

            case "m":
                await RequestToken("resource3.scope1", "urn:resource3");
                break;

            case "n":
                await RequestToken("resource3.scope1", "urn:resource2");
                break;
        }

        await app.StopAsync();
    }

    private static void OutputHelpContent()
    {
        "Resource setup:\n".ConsoleGreen();

        "resource1: resource1.scope1 resource1.scope2 shared.scope".ConsoleGreen();
        "resource2: resource2.scope1 resource2.scope2 shared.scope\n".ConsoleGreen();
        "resource3 (isolated): resource3.scope1 resource3.scope2 shared.scope\n".ConsoleGreen();
        "scopes without resource association: scope3 scope4 transaction\n\n".ConsoleGreen();

        // scopes without associated resource
        "a) scope3 scope4".ConsoleYellow();

        // one scope, single resource
        "b) resource1.scope1".ConsoleYellow();

        // two scopes, single resources
        "c) resource1.scope1 resource1.scope2".ConsoleYellow();

        // two scopes, one has a resource, one doesn't
        "d) resource1.scope1 scope3".ConsoleYellow();

        // two scopes, two resource
        "e) resource1.scope1 resource2.scope1".ConsoleYellow();

        // shared scope between two resources
        "f) shared.scope".ConsoleYellow();

        // shared scope between two resources and scope that belongs to resource
        "g) resource1.scope1 shared.scope".ConsoleYellow();

        // parameterized scope
        "h) transaction:123".ConsoleYellow();

        // no scope
        "i) no scope".ConsoleYellow();

        // no scope
        "j) no scope (resource: resource1)".ConsoleYellow();

        // no scope
        "k) no scope (resource: resource3)".ConsoleYellow();

        // isolated scope without resource parameter
        "l) resource3.scope1".ConsoleYellow();

        // isolated scope without resource parameter
        "m) resource3.scope1 (resource: resource3)".ConsoleYellow();

        // isolated scope without resource parameter
        "n) resource3.scope1 (resource: resource2)".ConsoleYellow();
    }

    static async Task RequestToken(string scope, string resource = null)
    {
        var client = new HttpClient();
        var disco = await Cache.GetAsync();

        var request = new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = "resources.and.scopes",
            ClientSecret = "secret",

            Scope = scope
        };

        if (!string.IsNullOrEmpty(resource))
        {
            request.Resource.Add(resource);
        }

        var response = await client.RequestClientCredentialsTokenAsync(request);

        if (response.IsError)
        {
            Console.WriteLine(response.Error);
            return;
        }

        Console.WriteLine();

        response.Show();
    }

    private static string ParseResource(string[] args)
    {
        Option<string> resourceOption = new("--resource")
        {
            Description = "Resource Selection"
        };

        var rootCommand = new RootCommand();
        rootCommand.Options.Add(resourceOption);

        var parseResult = rootCommand.Parse(args);
        if (parseResult.GetValue(resourceOption) is not string resource
            || string.IsNullOrWhiteSpace(resource))
        {
            throw new Exception("No valid string pat input to `--resource` argument.");
        }

        return resource.ToLower();
    }
}
