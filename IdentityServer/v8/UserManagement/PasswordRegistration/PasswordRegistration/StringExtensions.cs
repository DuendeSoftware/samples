// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace PasswordRegistration;

internal static class StringExtensions
{
    /// <summary>
    /// Takes an email string and puts it into the way it's stored in the database
    /// </summary>
    public static string NormalizeEmail(this string email) => email.ToLowerInvariant();
}
