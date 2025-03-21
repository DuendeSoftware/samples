// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using IdentityModel;

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
    /// Writes red text to the console.
    /// </summary>
    /// <param name="text">The text.</param>
    [DebuggerStepThrough]
    public static void ConsoleRed(this string text)
    {
        text.ColoredWriteLine(ConsoleColor.Red);
    }

    /// <summary>
    /// Writes yellow text to the console.
    /// </summary>
    /// <param name="text">The text.</param>
    [DebuggerStepThrough]
    public static void ConsoleYellow(this string text)
    {
        text.ColoredWriteLine(ConsoleColor.Yellow);
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

        Console.WriteLine(JsonSerializer.Serialize(JsonDocument.Parse(Encoding.UTF8.GetString(Base64Url.Decode(header))), new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(JsonDocument.Parse(Encoding.UTF8.GetString(Base64Url.Decode(payload))), new JsonSerializerOptions { WriteIndented = true }));
    }
}
