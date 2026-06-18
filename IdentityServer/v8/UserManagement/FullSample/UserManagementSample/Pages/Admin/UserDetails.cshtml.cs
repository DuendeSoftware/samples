// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Admin;

public sealed class UserDetailsModel(
    IUserProfileAdmin profileAdmin,
    IUserAuthenticatorsAdmin authenticatorsAdmin) : PageModel
{
    public UserSubjectId? SubjectId { get; set; }

    public Dictionary<string, string?> Attributes { get; set; } = [];

    public UserAuthenticatorsSummary? Authenticators { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string subjectId)
    {
        if (!UserSubjectId.TryCreate(subjectId, out var id))
        {
            ErrorMessage = "Invalid subject ID.";
            return Page();
        }

        SubjectId = id;

        var profile = await profileAdmin.TryGetAsync(id, HttpContext.RequestAborted);
        if (profile is null)
        {
            ErrorMessage = "User profile not found.";
            return Page();
        }

        foreach (var (code, value) in profile.Attributes)
        {
            Attributes[code.ToString()] = value.UntypedValue?.ToString();
        }

        var authenticators = await authenticatorsAdmin.TryGetAsync(id, HttpContext.RequestAborted);
        if (authenticators is not null)
        {
            Authenticators = new UserAuthenticatorsSummary
            {
                OtpAddresses = authenticators.OtpAddresses
                    .Select(a => $"{a.Channel}: {a.SubjectId}")
                    .ToList(),
                PasskeyCount = authenticators.Passkeys.Count,
                HasPassword = authenticators.HasPassword,
                TotpCount = authenticators.TotpDeviceNames.Count,
                ExternalProviders = authenticators.ExternalAuthenticatorAddresses
                    .Select(e => e.Name.ToString())
                    .ToList()
            };
        }

        return Page();
    }

    public sealed class UserAuthenticatorsSummary
    {
        public List<string> OtpAddresses { get; init; } = [];
        public int PasskeyCount { get; init; }
        public bool HasPassword { get; init; }
        public int TotpCount { get; init; }
        public List<string> ExternalProviders { get; init; } = [];
    }
}
