// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using AccountLockout.Services;
using Duende.IdentityModel;
using Duende.Storage.Querying;
using Duende.UserManagement;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountLockout.Pages.Admin;

[Authorize(Policy = Policies.Admin)]
public sealed class UsersModel(
    IUserProfileAdmin profileAdmin,
    AccountLockoutService accountLockoutService,
    TimeProvider timeProvider) : PageModel
{
    public IReadOnlyList<UserRow> Users { get; private set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var result = await profileAdmin.QueryAsync(QueryRequest.Empty, HttpContext.RequestAborted);
        var currentAdminSubjectId = User.FindFirst(JwtClaimTypes.Subject)?.Value;

        Users = [.. result.Items
            .Select(profile =>
            {
                var state = accountLockoutService.GetState(profile);
                return new UserRow
                {
                    SubjectId = profile.SubjectId.ToString(),
                    Email = profile.TryGetString(AccountLockoutProfileAttributes.Email.Code) ?? "(not available)",
                    Name = profile.TryGetString(AccountLockoutProfileAttributes.Name.Code) ?? "(not available)",
                    LockState = state.Kind switch
                    {
                        AccountLockoutKind.IndefinitelyLocked => "Indefinitely locked",
                        AccountLockoutKind.TemporarilyLocked => "Temporarily locked",
                        _ => "Unlocked"
                    },
                    LockStateCssClass = state.Kind switch
                    {
                        AccountLockoutKind.IndefinitelyLocked => "text-bg-danger",
                        AccountLockoutKind.TemporarilyLocked => "text-bg-warning",
                        _ => "text-bg-success"
                    },
                    LockedUntilUtc = state.LockedUntilUtc,
                    IsCurrentAdmin = string.Equals(profile.SubjectId.ToString(), currentAdminSubjectId, StringComparison.Ordinal)
                };
            })
            .OrderBy(user => user.Email, StringComparer.OrdinalIgnoreCase)];
    }

    public Task<IActionResult> OnPostLock15MinutesAsync(string subjectId) => ApplyLockAsync(subjectId, TimeSpan.FromMinutes(15), "Locked for 15 minutes.");
    public Task<IActionResult> OnPostLock1HourAsync(string subjectId) => ApplyLockAsync(subjectId, TimeSpan.FromHours(1), "Locked for 1 hour.");

    public async Task<IActionResult> OnPostLockCustomAsync(string subjectId, string? customLockedUntilUtc)
    {
        if (!TryValidateTarget(subjectId, isLockAction: true, out var target, out var failureResult))
        {
            return failureResult;
        }

        if (string.IsNullOrWhiteSpace(customLockedUntilUtc)
            || !customLockedUntilUtc.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
            || !DateTimeOffset.TryParse(customLockedUntilUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var lockedUntilUtc))
        {
            ErrorMessage = "Select a valid future lockout expiry.";
            return RedirectToPage();
        }

        return await ApplyResultAsync(
            await accountLockoutService.LockUntilAsync(target!, lockedUntilUtc, HttpContext.RequestAborted),
            $"Locked until {lockedUntilUtc.ToUniversalTime():yyyy-MM-dd HH:mm:ss 'UTC'}.");
    }

    public async Task<IActionResult> OnPostLockIndefinitelyAsync(string subjectId)
    {
        if (!TryValidateTarget(subjectId, isLockAction: true, out var target, out var failureResult))
        {
            return failureResult;
        }

        return await ApplyResultAsync(
            await accountLockoutService.LockIndefinitelyAsync(target!, HttpContext.RequestAborted),
            "Locked indefinitely.");
    }

    public async Task<IActionResult> OnPostUnlockAsync(string subjectId)
    {
        if (!TryValidateTarget(subjectId, isLockAction: false, out var target, out var failureResult))
        {
            return failureResult;
        }

        return await ApplyResultAsync(
            await accountLockoutService.UnlockAsync(target!, HttpContext.RequestAborted),
            "Account unlocked.");
    }

    private async Task<IActionResult> ApplyLockAsync(string subjectId, TimeSpan duration, string successMessage)
    {
        if (!TryValidateTarget(subjectId, isLockAction: true, out var target, out var failureResult))
        {
            return failureResult;
        }

        return await ApplyResultAsync(
            await accountLockoutService.LockUntilAsync(target!, timeProvider.GetUtcNow().Add(duration), HttpContext.RequestAborted),
            successMessage);
    }

    private Task<IActionResult> ApplyResultAsync(AccountLockoutMutationResult result, string successMessage)
    {
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "The requested change could not be applied.";
            return Task.FromResult<IActionResult>(RedirectToPage());
        }

        SuccessMessage = successMessage;
        return Task.FromResult<IActionResult>(RedirectToPage());
    }

    private bool TryValidateTarget(string subjectId, bool isLockAction, out UserSubjectId? target, out IActionResult failureResult)
    {
        target = null;

        if (!UserSubjectId.TryCreate(subjectId, out var parsedSubjectId))
        {
            ErrorMessage = "The selected user subject identifier was invalid.";
            failureResult = RedirectToPage();
            return false;
        }

        var currentAdminSubjectId = User.FindFirst(JwtClaimTypes.Subject)?.Value;

        // The disabled UI is only a convenience; enforce self-lockout prevention on the server.
        if (isLockAction && string.Equals(parsedSubjectId?.ToString(), currentAdminSubjectId, StringComparison.Ordinal))
        {
            ErrorMessage = "The current administrator cannot lock their own account.";
            failureResult = RedirectToPage();
            return false;
        }

        target = parsedSubjectId;
        failureResult = RedirectToPage();
        return true;
    }

    public sealed class UserRow
    {
        public required string SubjectId { get; init; }
        public required string Email { get; init; }
        public required string Name { get; init; }
        public required string LockState { get; init; }
        public required string LockStateCssClass { get; init; }
        public DateTimeOffset? LockedUntilUtc { get; init; }
        public bool IsCurrentAdmin { get; init; }
    }
}
