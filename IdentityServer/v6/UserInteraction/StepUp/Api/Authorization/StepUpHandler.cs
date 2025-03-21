// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Api.Authorization;

public class StepUpAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authResult)
    {
        // If the authorization was forbidden due to a step-up requirement, set
        // the status code and WWW-Authenticate header to indicate that step-up
        // is required
        if (authResult.Forbidden)
        {
            var maxAgeReq = authResult.AuthorizationFailure!.FailedRequirements
                .OfType<MaxAgeRequirement>().FirstOrDefault();
            var mfaReq = authResult.AuthorizationFailure!.FailedRequirements
               .OfType<ClaimsAuthorizationRequirement>()
               .FirstOrDefault(r => r.ClaimType == "amr" && r.AllowedValues!.Contains("mfa"));

            if (maxAgeReq != null || mfaReq != null)
            {
                var header = new StepUpWWWAuthenticateHeader();
                if (maxAgeReq != null)
                {
                    header.MaxAge = (int)maxAgeReq.MaxAge.TotalSeconds;
                }
                if (mfaReq != null)
                {
                    header.AcrValues = "mfa";
                }
                context.Response.Headers.WWWAuthenticate = header.ToString();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }
        // Fall back to the default implementation.
        await defaultHandler.HandleAsync(next, context, policy, authResult);
    }
}


public class StepUpWWWAuthenticateHeader
{
    private readonly string Error = "insufficient_user_authentication";
    private string ErrorDescription
    {
        get
        {
            var ret = string.Empty;
            if (MaxAge != null)
            {
                ret += "More recent authentication is required. ";
            }
            if (AcrValues != null)
            {
                ret += "MFA is required. ";
            }
            return ret;
        }
    }
    public int? MaxAge { get; set; }
    public string? AcrValues { get; set; }

    public override string ToString()
    {
        var props = new List<string> {
            $"Bearer error=\"{Error}\"",
            $"error_description=\"{ErrorDescription}\""
        };
        if (MaxAge != null)
        {
            props.Add($"max_age={MaxAge}");
        }
        if (AcrValues != null)
        {
            props.Add($"acr_values={AcrValues}");
        }
        return string.Join(',', props);
    }
}
