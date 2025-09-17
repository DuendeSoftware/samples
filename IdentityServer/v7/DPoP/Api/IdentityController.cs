// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("identity")]
public class IdentityController : ControllerBase
{
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(ILogger<IdentityController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult Get()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        _logger.LogInformation("claims: {claims}", claims);

        var scheme = GetAuthorizationScheme(Request);
        var proofToken = GetDPoPProofToken(Request);
        var accessToken = GetAccessToken(Request);

        return new JsonResult(new { scheme, proofToken, claims, accessToken });
    }

    public static string? GetAuthorizationScheme(HttpRequest request) =>
        request.Headers.Authorization.FirstOrDefault()?.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    public static string? GetDPoPProofToken(HttpRequest request) =>
        request.Headers[OidcConstants.HttpHeaders.DPoP].FirstOrDefault();

    public static string? GetAccessToken(HttpRequest request) =>
        request.Headers.Authorization.FirstOrDefault()?.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
}
