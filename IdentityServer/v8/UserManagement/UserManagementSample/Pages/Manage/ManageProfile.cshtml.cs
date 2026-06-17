// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

using Duende.IdentityModel;
using Duende.Storage.EntityAttributeValue;
using Duende.Storage.Internal.Outbox;
using Duende.UserManagement;
using Duende.UserManagement.Profiles;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Manage;

[Authorize]
public sealed class ManageProfileModel(
    IUserProfileSelfService profileSelfService) : PageModel
{
    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Url]
    public string? Website { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public List<string> ErrorMessages { get; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (GetUserId() is not { } userId)
        {
            return RedirectToPage("/Account/Login");
        }

        var profile = await profileSelfService.TryGetAsync(userId, HttpContext.RequestAborted);
        if (profile is null)
        {
            ErrorMessages.Add("Could not load profile. Please try again.");
            return Page();
        }

        if (profile.Attributes.TryGetValue(OidcStandardAttributes.Name.Code, out var nameAttribute)
            && nameAttribute.TryGetValue<string>(out var name))
        {
            Name = name ?? string.Empty;
        }

        if (profile.Attributes.TryGetValue(OidcStandardAttributes.Email.Code, out var emailAttribute)
            && emailAttribute.TryGetValue<string>(out var email))
        {
            Email = email ?? string.Empty;
        }

        if (profile.Attributes.TryGetValue(OidcStandardAttributes.Website.Code, out var websiteAttribute)
            && websiteAttribute.TryGetValue<string>(out var website))
        {
            Website = website;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (GetUserId() is not { } userId)
        {
            return RedirectToPage("/Account/Login");
        }

        var schema = await profileSelfService.GetSchemaAsync(HttpContext.RequestAborted);
        var attributes = new AttributeValueCollection(schema);

        attributes.Set(OidcStandardAttributes.Name.Code, Name);
        attributes.Set(OidcStandardAttributes.Email.Code, Email);

        if (!string.IsNullOrWhiteSpace(Website))
        {
            attributes.Set(OidcStandardAttributes.Website.Code, Website);
        }

        var updated = await profileSelfService.TryUpdateAsync(
            userId, attributes.Validate(), HttpContext.RequestAborted);

        if (updated is null)
        {
            ErrorMessages.Add("Could not update profile. Please try again.");
            return Page();
        }

        SuccessMessage = "Profile updated successfully.";
        return RedirectToPage();
    }

    private UserSubjectId? GetUserId()
    {
        if (User.FindFirst(JwtClaimTypes.Subject)?.Value is not { } sub)
        {
            return null;
        }

        return UserSubjectId.Create(sub);
    }
}
