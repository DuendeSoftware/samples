// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Buffers.Text;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Shared;

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

    public static void ShowAccessToken(this string accessToken)
    {
        var parts = accessToken.Split('.');
        var header = parts[0];
        var payload = parts[1];

        Console.WriteLine(JsonSerializer.Serialize(JsonDocument.Parse(Encoding.UTF8.GetString(Base64Url.DecodeFromChars(header))), new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(JsonDocument.Parse(Encoding.UTF8.GetString(Base64Url.DecodeFromChars(payload))), new JsonSerializerOptions { WriteIndented = true }));
    }
}
