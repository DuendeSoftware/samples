// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.UserManagement;
using Duende.UserManagement.Authentication.Passkeys;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TransportHints.Pages;

public class IndexModel(IPasskeyCeremonies ceremonies) : PageModel
{
    public async Task<IActionResult> OnPostVerifyAsync([FromBody] PasskeyCompleteAuthenticationRequest? request)
    {
        if (!UserSubjectId.TryCreate(User.FindFirst("sub")?.Value, out var subjectId))
        {
            return Unauthorized();
        }

        if (request is null || !ModelState.IsValid)
        {
            return BadRequest(new { detail = "The passkey response is invalid." });
        }

        var result = await ceremonies.CompleteAuthenticationAsync(request, HttpContext.RequestAborted);
        if (result is not PasskeyAuthenticationCompleteResult.Success success || success.UserSubjectId != subjectId)
        {
            return BadRequest(new { detail = "The passkey did not verify the signed-in account." });
        }

        // This demonstration confirms the ceremony without issuing or elevating a session.
        return new JsonResult(new { message = "Passkey verified for the signed-in account." });
    }
}
