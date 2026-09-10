// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Duende.IdentityServer.Validation;

namespace Storage;

internal sealed class AllowedRegionTokenRequestValidator(IRequestRegionResolver regionResolver)
    : ICustomTokenRequestValidator
{
    public Task ValidateAsync(CustomTokenRequestValidationContext context, CancellationToken _)
    {
        ArgumentNullException.ThrowIfNull(context.Result);

        var properties = context.Result.ValidatedRequest.Client.Properties;
        if (!properties.TryGetValue(SampleData.AllowedRegionAttribute.Code.Value, out var allowedRegion))
        {
            return Task.CompletedTask;
        }

        var requestedRegion = regionResolver.Resolve();
        if (!string.Equals(requestedRegion, allowedRegion, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new TokenRequestValidationResult(
                validatedRequest: context.Result.ValidatedRequest,
                error: OidcConstants.TokenErrors.InvalidRequest,
                errorDescription: $"The {SampleData.RegionHeaderName} header must match the client's allowed region."
            );
        }

        return Task.CompletedTask;
    }
}
