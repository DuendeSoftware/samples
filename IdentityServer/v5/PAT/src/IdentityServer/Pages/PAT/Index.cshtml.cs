// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.PAT
{
    [SecurityHeaders]
    [Authorize]
    public class Index : PageModel
    {
        private readonly ITokenService _tokenService;
        private readonly IIssuerNameService _issuerNameService;


        [BindProperty]
        public ViewModel View { get; set; }

        public string Token { get; set; }

        public Index(ITokenService tokenService, IIssuerNameService issuerNameService)
        {
            _tokenService = tokenService;
            _issuerNameService = issuerNameService;
        }

        public void OnGet()
        {
            View = new ViewModel();
        }

        public async Task<IActionResult> OnPost()
        {
            var token = new Token(IdentityServerConstants.TokenTypes.AccessToken)
            {
                Issuer = await _issuerNameService.GetCurrentAsync(),
                Lifetime = Convert.ToInt32(TimeSpan.FromDays(View.LifetimeDays).TotalSeconds),
                CreationTime = DateTime.UtcNow,
                ClientId = "pat.client",

                Claims = new List<Claim>
                {
                    new("client_id", "pat.client"),
                    new("sub", User.GetSubjectId())
                },

                AccessTokenType = View.IsReferenceToken ? AccessTokenType.Reference : AccessTokenType.Jwt
            };


            if (View.ForApi1)
            {
                token.Audiences.Add("api1");
                token.Claims.Add(new("scope", "scope1"));
            }

            if (View.ForApi2)
            {
                token.Audiences.Add("api2");
                token.Claims.Add(new("scope", "scope2"));
            }

            Token = await _tokenService.CreateSecurityTokenAsync(token);
            return Page();
        }
    }
}
