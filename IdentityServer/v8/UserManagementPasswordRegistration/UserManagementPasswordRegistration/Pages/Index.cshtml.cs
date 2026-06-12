// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.UserManagement.Profiles;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.Security.Claims;

namespace UserManagementPasswordRegistration.Pages;

public class IndexModel(IUserProfileSelfService profileSelfService) : PageModel
{
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? FavoriteDinosaur { get; set; }

    public string NameDescription { get; } = UserAttributes.Name.Description?.ToString() ?? "";
    public string FavoriteDinosaurDescription { get; } = UserAttributes.FavoriteDinosaur.Description?.ToString() ?? "";

    public async Task<IActionResult> OnGetAsync()
    {
        LoadModel();
        if (!string.IsNullOrWhiteSpace(Email))
        {
            var profile = await profileSelfService.TryGetAsync(User.GetSubjectId(), HttpContext.RequestAborted);
            if (profile is not null)
            {
                if (profile.Attributes.GetValueOrDefault(UserAttributes.Name.Code)?.TryGetValue<string>(out var name) == true)
                {
                    Name = name;
                }

                if (profile.Attributes.GetValueOrDefault(UserAttributes.FavoriteDinosaur.Code)?.TryGetValue<string>(out var favoriteDinosaur) == true)
                {
                    FavoriteDinosaur = favoriteDinosaur;
                }
            }
        }

        return Page();
    }

    private void LoadModel()
    {
        if (User.FindFirst(JwtClaimTypes.Email) is not null)
        {
            Email = User.FindFirst(JwtClaimTypes.Email)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
