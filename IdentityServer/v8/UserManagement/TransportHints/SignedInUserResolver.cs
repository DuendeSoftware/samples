// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.UserManagement;
using Duende.UserManagement.Authentication.Passkeys;

namespace TransportHints;

public sealed class SignedInUserResolver(IHttpContextAccessor contextAccessor) : ISecondFactorPasskeyAuthenticationResolver
{
    public Task<UserSubjectId?> ResolveAsync(CancellationToken ct)
    {
        var user = contextAccessor.HttpContext?.User;
        var subject = user?.FindFirst("sub")?.Value;
        return Task.FromResult<UserSubjectId?>(
            user?.Identity?.IsAuthenticated == true && UserSubjectId.TryCreate(subject, out var subjectId)
                ? subjectId
                : null);
    }
}
