// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;

namespace Client;

public static class TokenResponseExtensions
{
    public static void Show(this TokenResponse response)
    {
        if (!response.IsError)
        {
            "Token response:".ConsoleGreen();
            Console.WriteLine(response.Json);

            if (response.AccessToken.Contains("."))
            {
                "\nAccess Token (decoded):".ConsoleGreen();

                var parts = response.AccessToken.Split('.');
                var header = parts[0];
                var claims = parts[1];

                Console.WriteLine(PrettyPrintJson(Encoding.UTF8.GetString(Base64Url.Decode(header))));
                Console.WriteLine(PrettyPrintJson(Encoding.UTF8.GetString(Base64Url.Decode(claims))));
            }
        }
        else
        {
            if (response.ErrorType == ResponseErrorType.Http)
            {
                "HTTP error: ".ConsoleGreen();
                Console.WriteLine(response.Error);
                "HTTP status code: ".ConsoleGreen();
                Console.WriteLine(response.HttpStatusCode);
            }
            else
            {
                "Protocol error response:".ConsoleGreen();
                Console.WriteLine(response.Raw);
            }
        }
    }

    public static string PrettyPrintJson(this string raw)
    {
        var doc = JsonDocument.Parse(raw).RootElement;
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}


public static class ConsoleExtensions
{
    /// <summary>
    /// Writes green text to the console.
    /// </summary>
    /// <param name="text">The text.</param>
    [DebuggerStepThrough]
    public static void ConsoleGreen(this string text)
    {
        text.ColoredWriteLine(ConsoleColor.Green);
    }


    /// <summary>
    /// Writes out text with the specified ConsoleColor.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="color">The color.</param>
    [DebuggerStepThrough]
    public static void ColoredWriteLine(this string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}
