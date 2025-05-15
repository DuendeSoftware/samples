// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Duende.IdentityModel;

namespace IdentityServerHost;

public class StepUpInteractionResponseGenerator : AuthorizeInteractionResponseGenerator
{
    public StepUpInteractionResponseGenerator(
        IdentityServerOptions options,
        IClock clock,
        ILogger<AuthorizeInteractionResponseGenerator> logger,
        IConsentService consent,
        IProfileService profile) : base(options, clock, logger, consent, profile)
    {
    }

    protected override async Task<InteractionResponse> ProcessLoginAsync(ValidatedAuthorizeRequest request)
    {
        var result = await base.ProcessLoginAsync(request);

        if (!result.IsLogin && !result.IsError)
        {
            if (MfaRequired(request) && !AuthenticatedWithMfa(request.Subject))
            {
                if (UserDeclinedMfa(request.Subject))
                {
                    result.Error = OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements;
                }
                else
                {
                    result.RedirectUrl = "/Account/Mfa";
                }
            }
        }
        return result;
    }

    private bool MfaRequired(ValidatedAuthorizeRequest request) =>
        MfaRequestedByClient(request) ||
        AlwaysUseMfaForUser(request.Subject.Identity.Name);

    private bool MfaRequestedByClient(ValidatedAuthorizeRequest request)
    {
        return request.AuthenticationContextReferenceClasses.Contains("mfa");
    }

    // If you have the requirement that some users will always use MFA and
    // others will not, you could implement that here. This might be a user
    // controlled option, or set according to some business logic.
    private bool AlwaysUseMfaForUser(string sub)
    {
        return sub == "bob";
    }

    private bool AuthenticatedWithMfa(ClaimsPrincipal user) =>
        user.Claims.Any(c => c.Type == "amr" && c.Value == "mfa");

    private bool UserDeclinedMfa(ClaimsPrincipal user) =>
        user.Claims.Any(c => c.Type == "declined_mfa" && c.Value == "true");
}
