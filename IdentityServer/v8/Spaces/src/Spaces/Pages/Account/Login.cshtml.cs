// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Stores;
using Duende.Spaces;
using Duende.UserManagement;
using Duende.UserManagement.Authentication.Passwords;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spaces.Seed;

namespace Spaces.Pages.Account;

public class LoginModel(
    IPasswordAuthenticator passwordAuth,
    IUserProfileSelfService profileSelfService,
    ISpaceContextAccessor spaceContextAccessor,
    ISpaceStore spaceStore,
    IIdentityProviderStore identityProviders,
    IConfiguration configuration) : PageModel
{
    [BindProperty, Required]
    public string Username { get; set; } = string.Empty;

    [BindProperty, Required]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    /// <summary>Demo username for the current space, or null if no demo user is seeded.</summary>
    public string? DemoUsername { get; private set; }

    /// <summary>Demo password (shared across all spaces).</summary>
    public string DemoPassword => configuration["Demo:Password"] ?? DemoCredentials.Password;

    public string SpaceName { get; private set; } = "Default";

    public List<string> Schemes { get; private set; } = new();

    public async Task OnGet(string? returnUrl, CancellationToken ct)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        await PopulateViewState(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await PopulateViewState(ct);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!NonValidatedPassword.TryCreate(Password, out var passwordValue))
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        var result = await passwordAuth.TryAuthenticateAsync(
            DemoUserAttributes.UserName,
            Username,
            passwordValue,
            HttpContext.RequestAborted);

        return result switch
        {
            PasswordAuthenticationResult.Success success => await CompleteSignIn(success.UserSubjectId),
            PasswordAuthenticationResult.Expired expired => await CompleteSignIn(expired.UserSubjectId),
            PasswordAuthenticationResult.Failure => Fail("Invalid username or password."),
            _ => Fail("Unexpected authentication result.")
        };
    }

    public async Task<IActionResult> OnPostExternalLoginAsync(string provider, CancellationToken ct)
    {
        await PopulateViewState(ct);

        // Validate provider against current space's registered schemes
        if (!Schemes.Contains(provider))
        {
            return Fail($"External provider '{provider}' is not available for this space.");
        }

        var callbackUrl = Url.Page(
            "/Account/ExternalLoginCallback",
            values: new { returnUrl = ReturnUrl });

        var properties = new AuthenticationProperties
        {
            RedirectUri = callbackUrl
        };

        return Challenge(properties, provider);
    }

    private async Task<IActionResult> CompleteSignIn(UserSubjectId subjectId)
    {
        var ct = HttpContext.RequestAborted;
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.AuthenticationMethod, OidcConstants.AuthenticationMethods.Password)
        };

        // Load profile attributes (name, email, etc.) and add as claims
        var profile = await profileSelfService.TryGetAsync(subjectId, ct);
        if (profile is not null)
        {
            foreach (var (code, value) in profile.Attributes)
            {
                var val = value.UntypedValue?.ToString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    claims.Add(new Claim(code.ToString(), val));
                }
            }
        }

        var space = await spaceStore.TryGetSpace(spaceContextAccessor.GetSpaceId(), ct);
        claims.Add(new Claim("space", space?.Name ?? "Default"));

        var identityServerUser = new IdentityServerUser(subjectId.Value)
        {
            AdditionalClaims = claims
        };

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            IssuedUtc = DateTimeOffset.UtcNow,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(identityServerUser, authProperties);

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Content("~/"));
    }

    private IActionResult Fail(string message)
    {
        ErrorMessage = message;
        return Page();
    }

    private async Task PopulateViewState(CancellationToken ct)
    {
        var spaceId = spaceContextAccessor.GetSpaceId();
        var space = await spaceStore.TryGetSpace(spaceId, ct);
        var spaceName = space?.Name ?? "Default";
        // The space name is encoded in the space id for non-default spaces
        SpaceName = spaceName;
        DemoUsername = DemoCredentials.GetUsernameForSpace(spaceName);

        var schemes = await identityProviders.GetAllSchemeNamesAsync(ct);

        Schemes = schemes.Select(x => x.Scheme).ToList();
    }
}
