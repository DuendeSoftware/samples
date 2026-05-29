// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages;

[Authorize]
public sealed class ConsentModel : PageModel
{
    private static readonly bool EnableOfflineAccess = true;
    private static readonly string OfflineAccessDisplayName = "Offline Access";
    private static readonly string OfflineAccessDescription = "Access to your applications and resources, even when you are offline";
    private static readonly string MustChooseOneErrorMessage = "You must pick at least one permission";
    private static readonly string InvalidSelectionErrorMessage = "Invalid selection";

    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    private readonly ILogger<ConsentModel> _logger;

    public ConsentModel(
        IIdentityServerInteractionService interaction,
        IEventService events,
        ILogger<ConsentModel> logger)
    {
        _interaction = interaction;
        _events = events;
        _logger = logger;
    }

    public ConsentViewModel View { get; set; } = default!;

    [BindProperty]
    public ConsentInputModel Input { get; set; } = default!;

    public async Task<IActionResult> OnGet(string? returnUrl)
    {
        if (!await SetViewModelAsync(returnUrl))
        {
            return RedirectToPage("/Index");
        }

        Input = new ConsentInputModel
        {
            ReturnUrl = returnUrl,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var request = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl, HttpContext.RequestAborted);
        if (request == null)
        {
            return RedirectToPage("/Index");
        }

        ConsentResponse? grantedConsent = null;

        if (Input.Button == "no")
        {
            grantedConsent = new ConsentResponse { Error = InteractionError.AccessDenied };
            await _events.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues), HttpContext.RequestAborted);
        }
        else if (Input.Button == "yes")
        {
            if (Input.ScopesConsented.Any())
            {
                var scopes = Input.ScopesConsented;
                if (!EnableOfflineAccess)
                {
                    scopes = scopes.Where(x => x != Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess);
                }

                grantedConsent = new ConsentResponse
                {
                    RememberConsent = Input.RememberConsent,
                    ScopesValuesConsented = scopes.ToArray(),
                    Description = Input.Description
                };

                await _events.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented, grantedConsent.RememberConsent), HttpContext.RequestAborted);
            }
            else
            {
                ModelState.AddModelError(string.Empty, MustChooseOneErrorMessage);
            }
        }
        else
        {
            ModelState.AddModelError(string.Empty, InvalidSelectionErrorMessage);
        }

        if (grantedConsent != null)
        {
            ArgumentNullException.ThrowIfNull(Input.ReturnUrl, nameof(Input.ReturnUrl));

            await _interaction.GrantConsentAsync(request, grantedConsent, HttpContext.RequestAborted);

            return Redirect(Input.ReturnUrl);
        }

        if (!await SetViewModelAsync(Input.ReturnUrl))
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    private async Task<bool> SetViewModelAsync(string? returnUrl)
    {
        if (returnUrl == null)
        {
            return false;
        }

        var request = await _interaction.GetAuthorizationContextAsync(returnUrl, HttpContext.RequestAborted);
        if (request != null)
        {
            View = CreateConsentViewModel(request);
            return true;
        }

        _logger.LogWarning("No consent request matching request: {ReturnUrl}", returnUrl);
        return false;
    }

    private ConsentViewModel CreateConsentViewModel(AuthorizationRequest request)
    {
        var vm = new ConsentViewModel
        {
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            ClientUrl = request.Client.ClientUri,
            ClientLogoUrl = request.Client.LogoUri,
            AllowRememberConsent = request.Client.AllowRememberConsent
        };

        vm.IdentityScopes = request.ValidatedResources.Resources.IdentityResources
            .Select(x => CreateScopeViewModel(x, Input == null || Input.ScopesConsented.Contains(x.Name)))
            .ToArray();

        var resourceIndicators = request.Parameters.GetValues(OidcConstants.AuthorizeRequest.Resource) ?? Enumerable.Empty<string>();
        var apiResources = request.ValidatedResources.Resources.ApiResources.Where(x => resourceIndicators.Contains(x.Name));

        var apiScopes = new List<ConsentScopeViewModel>();
        foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
        {
            var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
            if (apiScope != null)
            {
                var scopeVm = CreateScopeViewModel(parsedScope, apiScope, Input == null || Input.ScopesConsented.Contains(parsedScope.RawValue));
                scopeVm.Resources = apiResources
                    .Where(x => x.Scopes.Contains(parsedScope.ParsedName))
                    .Select(x => new ConsentResourceViewModel { Name = x.Name, DisplayName = x.DisplayName ?? x.Name })
                    .ToArray();
                apiScopes.Add(scopeVm);
            }
        }

        if (EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess)
        {
            apiScopes.Add(CreateOfflineAccessScope(Input == null || Input.ScopesConsented.Contains(Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess)));
        }

        vm.ApiScopes = apiScopes;
        return vm;
    }

    private static ConsentScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check)
    {
        return new ConsentScopeViewModel
        {
            Name = identity.Name,
            Value = identity.Name,
            DisplayName = identity.DisplayName ?? identity.Name,
            Description = identity.Description,
            Emphasize = identity.Emphasize,
            Required = identity.Required,
            Checked = check || identity.Required
        };
    }

    private static ConsentScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check)
    {
        var displayName = apiScope.DisplayName ?? apiScope.Name;
        if (!string.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter))
        {
            displayName += ":" + parsedScopeValue.ParsedParameter;
        }

        return new ConsentScopeViewModel
        {
            Name = parsedScopeValue.ParsedName,
            Value = parsedScopeValue.RawValue,
            DisplayName = displayName,
            Description = apiScope.Description,
            Emphasize = apiScope.Emphasize,
            Required = apiScope.Required,
            Checked = check || apiScope.Required
        };
    }

    private static ConsentScopeViewModel CreateOfflineAccessScope(bool check)
    {
        return new ConsentScopeViewModel
        {
            Value = Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess,
            DisplayName = OfflineAccessDisplayName,
            Description = OfflineAccessDescription,
            Emphasize = true,
            Checked = check
        };
    }
}

public sealed class ConsentViewModel
{
    public string? ClientName { get; set; }
    public string? ClientUrl { get; set; }
    public string? ClientLogoUrl { get; set; }
    public bool AllowRememberConsent { get; set; }
    public IEnumerable<ConsentScopeViewModel> IdentityScopes { get; set; } = Enumerable.Empty<ConsentScopeViewModel>();
    public IEnumerable<ConsentScopeViewModel> ApiScopes { get; set; } = Enumerable.Empty<ConsentScopeViewModel>();
}

public sealed class ConsentScopeViewModel
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool Emphasize { get; set; }
    public bool Required { get; set; }
    public bool Checked { get; set; }
    public IEnumerable<ConsentResourceViewModel> Resources { get; set; } = Enumerable.Empty<ConsentResourceViewModel>();
}

public sealed class ConsentInputModel
{
    public string? Button { get; set; }
    public IEnumerable<string> ScopesConsented { get; set; } = new List<string>();
    public bool RememberConsent { get; set; } = true;
    public string? ReturnUrl { get; set; }
    public string? Description { get; set; }
}

public sealed class ConsentResourceViewModel
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
}
