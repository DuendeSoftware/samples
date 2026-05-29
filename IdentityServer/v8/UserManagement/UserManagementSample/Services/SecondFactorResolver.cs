// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.UserManagement;
using Duende.UserManagement.Authentication.Passkeys;

namespace UserManagementSample.Services;

/// <summary>
/// Resolves the user for passkey second-factor authentication by reading the
/// interim subject ID stored after a successful first-factor (password) login.
/// </summary>
internal sealed class SecondFactorResolver(TotpStateCookie totpStateCookie)
    : ISecondFactorPasskeyAuthenticationResolver
{
    public Task<UserSubjectId?> ResolveAsync(CancellationToken ct)
    {
        UserSubjectId? subjectId = totpStateCookie.TryRead(out var id) ? id : null;
        return Task.FromResult(subjectId);
    }
}
