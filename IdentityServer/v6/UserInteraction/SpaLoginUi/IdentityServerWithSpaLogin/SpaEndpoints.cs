// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Spa;

public class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; }
    [Required]
    [MaxLength(100)]
    public string Password { get; set; }
    public bool Remember { get; set; }
    [MaxLength(2000)]
    public string ReturnUrl { get; set; }
}

public class ConsentRequest
{
    public bool Deny { get; set; }
    public bool Remember { get; set; }
    [MaxLength(2000)]
    public string ReturnUrl { get; set; }
}

public class LoginConsentResponse
{
    public string Error { get; set; }
    public string ValidReturnUrl { get; set; }
}

[Route("spa")]
[EnableCors("spa")]
public class SpaEndpoints : ControllerBase
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IServerUrls _serverUrls;
    private readonly TestUserStore _users;

    public SpaEndpoints(IIdentityServerInteractionService interaction, IServerUrls serverUrls)
    {
        _interaction = interaction;
        _serverUrls = serverUrls;
        _users = new TestUserStore(TestUsers.Users);
    }

    [HttpGet("context")]
    public async Task<IActionResult> Context(string returnUrl)
    {
        var authzContext = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (authzContext != null)
        {
            return Ok(new
            {
                loginHint = authzContext.LoginHint,
                idp = authzContext.IdP,
                tenant = authzContext.Tenant,
                scopes = authzContext.ValidatedResources.RawScopeValues,
                client = authzContext.Client.ClientName ?? authzContext.Client.ClientId
            });
        }

        return BadRequest();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        var response = new LoginConsentResponse();

        if (ModelState.IsValid && _users.ValidateCredentials(model.Username, model.Password))
        {
            var url = model.ReturnUrl != null ? Uri.UnescapeDataString(model.ReturnUrl) : null;

            var authzContext = await _interaction.GetAuthorizationContextAsync(url);
            if (authzContext != null)
            {
                response.ValidReturnUrl = url;
            }
            else
            {
                response.ValidReturnUrl = _serverUrls.BaseUrl;
            }

            var user = _users.FindByUsername(model.Username);
            var isUser = new IdentityServerUser(user.SubjectId)
            {
                DisplayName = user.Username,
            };

            var props = new AuthenticationProperties
            {
                IsPersistent = model.Remember
            };

            await HttpContext.SignInAsync(isUser.CreatePrincipal(), props);

            return Ok(response);
        }

        response.Error = "invalid username or password";
        return new BadRequestObjectResult(response);
    }

    [HttpPost("consent")]
    public async Task<IActionResult> Consent([FromBody] ConsentRequest model)
    {
        var response = new LoginConsentResponse();

        if (ModelState.IsValid)
        {
            var url = Uri.UnescapeDataString(model.ReturnUrl);

            var authzContext = await _interaction.GetAuthorizationContextAsync(url);
            if (authzContext != null)
            {
                response.ValidReturnUrl = url;

                if (model.Deny)
                {
                    await _interaction.DenyAuthorizationAsync(authzContext, AuthorizationError.AccessDenied);
                }
                else
                {
                    await _interaction.GrantConsentAsync(authzContext,
                        new ConsentResponse
                        {
                            RememberConsent = model.Remember,
                            ScopesValuesConsented = authzContext.ValidatedResources.RawScopeValues
                        });
                }

                return Ok(response);
            }
        }

        response.Error = "error";
        return new BadRequestObjectResult(response);
    }

    [HttpGet("error")]
    public async Task<IActionResult> Error(string errorId)
    {
        var errorInfo = await _interaction.GetErrorContextAsync(errorId);
        return Ok(new
        {
            errorInfo.Error,
            errorInfo.ErrorDescription
        });
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout(string logoutId)
    {
        var logoutInfo = await _interaction.GetLogoutContextAsync(logoutId);

        if (logoutInfo != null)
        {
            if (!logoutInfo.ShowSignoutPrompt || !User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync();

                return Ok(new
                {
                    iframeUrl = logoutInfo.SignOutIFrameUrl,
                    postLogoutRedirectUri = logoutInfo.PostLogoutRedirectUri
                });
            }

        }

        return Ok(new
        {
            prompt = User.Identity.IsAuthenticated
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> PostLogout(string logoutId)
    {
        var logoutInfo = await _interaction.GetLogoutContextAsync(logoutId);

        await HttpContext.SignOutAsync();

        return Ok(new
        {
            iframeUrl = logoutInfo?.SignOutIFrameUrl,
            postLogoutRedirectUri = logoutInfo?.PostLogoutRedirectUri
        });
    }
}
