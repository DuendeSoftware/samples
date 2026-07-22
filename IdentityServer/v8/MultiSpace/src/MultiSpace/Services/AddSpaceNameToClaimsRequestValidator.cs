// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityServer.Validation;
using Duende.MultiSpace;

namespace MultiSpace.Services;

public class AddSpaceNameToClaimsRequestValidator(ISpaceContextAccessor spaceContextAccessor, ISpaceStore spaceStore)
    : ICustomTokenRequestValidator
{
    public async Task ValidateAsync(CustomTokenRequestValidationContext context, CancellationToken ct)
    {
        var request = context.Result?.ValidatedRequest;
        if (request == null)
        {
            return;
        }

        var space = await spaceStore.TryGetSpace(spaceContextAccessor.GetSpaceId(), ct);
        request.ClientClaims.Add(new Claim("space", space?.Name ?? "default"));
    }
}
