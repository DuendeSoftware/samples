// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.Validation;

namespace IdentityServerHost;

// shows how to access the parsed scope in the token request pipeline
public class TokenRequestValidator : ICustomTokenRequestValidator
{
    public Task ValidateAsync(CustomTokenRequestValidationContext context)
    {
        var transaction =
            context.Result.ValidatedRequest.ValidatedResources?.ParsedScopes.FirstOrDefault(x =>
                x.ParsedName == "transaction");

        if (transaction?.ParsedParameter != null)
        {
            context.Result.ValidatedRequest.ClientClaims.Add(new Claim("transaction_id",
                transaction.ParsedParameter));
        }

        return Task.CompletedTask;
    }
}
