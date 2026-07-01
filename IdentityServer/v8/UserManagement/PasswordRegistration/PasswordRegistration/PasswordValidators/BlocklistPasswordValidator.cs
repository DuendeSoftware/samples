// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.UserManagement;
using Duende.UserManagement.Authentication.Passwords;

namespace PasswordRegistration.PasswordValidators;

/// <summary>
/// A sample <see cref="IPasswordValidator"/> that rejects "commonly used" passwords from a hard coded list.
/// </summary>
public sealed class BlocklistPasswordValidator : IPasswordValidator
{
    private static readonly HashSet<string> Blocklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "123456", "123456789", "qwerty", "abc123",
        "monkey", "1234567", "letmein", "trustno1", "dragon",
        "baseball", "iloveyou", "master", "sunshine", "ashley",
        "bailey", "passw0rd", "shadow", "123123", "654321",
        "Pass123!!!"
    };

    public Task<PasswordValidationResult> ValidateAsync(UserSubjectId userId, string password, CancellationToken ct) =>
        Task.FromResult<PasswordValidationResult>(
            Blocklist.Contains(password)
                ? new PasswordValidationResult.Rejected("Password appears in common password blocklist")
                : new PasswordValidationResult.Accepted());
}
