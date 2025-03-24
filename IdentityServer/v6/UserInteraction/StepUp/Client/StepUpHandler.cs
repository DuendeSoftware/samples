// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication;

public class StepUpHandler : DelegatingHandler
{
    public StepUpHandler(IHttpContextAccessor accessor)
    {
        _contextAccessor = accessor;
    }

    private readonly IHttpContextAccessor _contextAccessor;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.Headers.Contains("WWW-Authenticate"))
        {
            var authParam = response.Headers.WwwAuthenticate.First().Parameter;
            if (string.IsNullOrEmpty(authParam))
            {
                return response;
            }

            var attributes = ParseWwwAuthenticateParameter(authParam);

            var props = new AuthenticationProperties();

            if (attributes.TryGetValue("max_age", out var maxAge))
            {
                props.Items.Add("max_age", maxAge);
            }
            if (attributes.TryGetValue("acr_values", out var acrValues))
            {
                props.Items.Add("acr_values", acrValues);
            }

            var httpContext = _contextAccessor.HttpContext;
            if (props.Items.Any())
            {
                await httpContext!.ChallengeAsync("oidc", props);
            }
        }

        return response;
    }

    private Dictionary<string, string> ParseWwwAuthenticateParameter(string parameter)
    {
        return parameter
            .Split(',')
            .Select(a => a.Trim())
            .Select(a => a.Split('=').Select(x => x.Trim()).ToList())
            .ToDictionary(a => a[0], a => a[1]);
    }
}
