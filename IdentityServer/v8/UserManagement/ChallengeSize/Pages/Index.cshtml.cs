// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passkeys;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ChallengeSize.Pages;

public class IndexModel(
    IPasskeyCeremonies ceremonies,
    IOptions<UserAuthenticationOptions> options,
    ILogger<IndexModel> logger) : PageModel
{
    public int ConfiguredSize => options.Value.Passkeys.ChallengeSize;
    public string? EncodedChallenge { get; private set; }
    public int? ByteLength => EncodedChallenge is null ? null : WebEncoders.Base64UrlDecode(EncodedChallenge).Length;
    public string? CeremonyType { get; private set; }

    public Task<IActionResult> OnPostRegistrationAsync() => GenerateAsync(registration: true);
    public Task<IActionResult> OnPostAuthenticationAsync() => GenerateAsync(registration: false);

    private async Task<IActionResult> GenerateAsync(bool registration)
    {
        CeremonyType = registration ? "Registration" : "Discoverable authentication";
        if (registration)
        {
            var session = await ceremonies.BeginRegistrationAsync(
                UserSubjectId.Create("challenge-size-demo"),
                "demo",
                "Challenge Size Demo",
                HttpContext.RequestAborted
            );

            EncodedChallenge = session.Options.Challenge;
        }
        else
        {
            var result = await ceremonies.BeginAuthenticationAsync(HttpContext.RequestAborted);
            if (result is PasskeyAuthenticationBeginResult.Success success)
            {
                EncodedChallenge = success.Session.Options.Challenge;
            }
            else if (result is PasskeyAuthenticationBeginResult.Failure failure)
            {
                logger.LogWarning("Passkey begin failed: {Error}: {Description}", failure.Error, failure.ErrorDescription);
                ModelState.AddModelError(string.Empty, failure.ErrorDescription);
                Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        return Page();
    }
}
