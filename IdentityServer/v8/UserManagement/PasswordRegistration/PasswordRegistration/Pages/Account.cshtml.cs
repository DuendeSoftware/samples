// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement.Profiles;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.Security.Claims;

namespace PasswordRegistration.Pages;

[Authorize]
public class AccountModel(IUserProfileSelfService profileSelfService) : PageModel
{
    public string? Email { get; set; }

    [BindProperty] public string? Name { get; set; }

    [BindProperty] public string? FavoriteDinosaur { get; set; }

    public string NameDescription { get; } = UserAttributes.Name.Description?.ToString() ?? "";
    public string FavoriteDinosaurDescription { get; } = UserAttributes.FavoriteDinosaur.Description?.ToString() ?? "";

    [TempData] public string? SuccessMessage { get; set; }

    public List<string> ErrorMessages { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        LoadModel();

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

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadModel();
            return Page();
        }

        var schema = await profileSelfService.GetSchemaAsync(HttpContext.RequestAborted);

        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(nameof(Name), "Invalid name format");
            LoadModel();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(FavoriteDinosaur))
        {
            ModelState.AddModelError(nameof(FavoriteDinosaur), "Invalid favorite dinosaur format");
            LoadModel();
            return Page();
        }

        var subjectId = User.GetSubjectId();

        if (await profileSelfService.TryGetAsync(subjectId, HttpContext.RequestAborted) is not { } profile)
        {
            ModelState.AddModelError(string.Empty, "Profile does not exist");
            LoadModel();
            return Page();
        }
        else
        {
            //Load all atrributes for the user profile, and then modify
            var updatedAttributes = new AttributeValueCollection(schema, profile.Attributes.Values);
            updatedAttributes.Set(UserAttributes.Name, Name);
            updatedAttributes.Set(UserAttributes.FavoriteDinosaur, FavoriteDinosaur);

            if (!updatedAttributes.TryValidate(out var validatedUpdatedAttributes, out var errors))
            {
                var errorsString = string.Join(", ", errors);
                ErrorMessages.Add($"Failed to update profile. Please try again. Errors; {errorsString}");
                LoadModel();
                return Page();
            }

            if (await profileSelfService.TryUpdateAsync(profile.SubjectId, validatedUpdatedAttributes, HttpContext.RequestAborted) is null)
            {
                ErrorMessages.Add("Failed to update profile. Please try again.");
                LoadModel();
                return Page();
            }

            SuccessMessage = "Profile updated!";
        }

        await HttpContext.SignInAsync(User);
        return RedirectToPage();
    }

    private void LoadModel()
    {
        if (User.FindFirst(JwtClaimTypes.Email) is not null)
        {
            Email = User.FindFirst(JwtClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
