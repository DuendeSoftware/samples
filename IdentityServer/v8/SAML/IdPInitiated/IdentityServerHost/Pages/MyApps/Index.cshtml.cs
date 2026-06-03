// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Saml;
using Duende.IdentityServer.Stores;
using IdentityServerHost.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.MyApps;

[SecurityHeaders]
[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IIdpInitiatedSsoService _ssoService;
    private readonly ISamlServiceProviderStore _spStore;

    public IndexModel(IIdpInitiatedSsoService ssoService, ISamlServiceProviderStore spStore)
    {
        _ssoService = ssoService;
        _spStore = spStore;
    }

    public List<AppTile> Apps { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        await foreach (var sp in _spStore.GetAllSamlServiceProvidersAsync(ct))
        {
            if (sp.AllowIdpInitiated)
            {
                Apps.Add(new AppTile(sp.EntityId, sp.DisplayName ?? sp.EntityId));
            }
        }
    }

    public async Task<IActionResult> OnPostLaunchAsync(string spEntityId, CancellationToken ct)
    {
        var result = await _ssoService.CreateResponseAsync(HttpContext, spEntityId, ct);

        if (result.IsError)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "An error occurred launching the application.");
            await OnGetAsync(ct);
            return Page();
        }

        if (result.Response is null)
        {
            ModelState.AddModelError(string.Empty, "Unexpected error: no SAML response was generated.");
            await OnGetAsync(ct);
            return Page();
        }

        // The SDK's CreateResponseAsync returns an IResult (a minimal API type) that writes
        // a SAML auto-POST form to the HTTP response. Since we're in a Razor Page handler
        // (which expects an IActionResult), we bridge between the two by executing the IResult
        // directly against the HttpContext and then returning EmptyResult to prevent the
        // Razor Pages framework from also writing to the response.
        await result.Response.ExecuteAsync(HttpContext);
        return new EmptyResult();
    }
}

public sealed record AppTile(string EntityId, string DisplayName);
