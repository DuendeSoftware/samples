// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace PasswordRegistration;

internal static class StringExtensions
{
    /// Duende User Management respects the email RFC, where the part before the @ is case-sensitive
    /// and the domain itself is not.
    ///
    /// aaa@aaa.com and AaA@aaa.com are different email addresses.
    ///
    /// In this sample, we are normalizing the email address so that the sample app treats
    /// these 2 example emails as the same.
    /// 
    /// While it differs from the RFC, it's what users expect.
    public static string NormalizeEmail(this string email) => email.ToLowerInvariant();
}
