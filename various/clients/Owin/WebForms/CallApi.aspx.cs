using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Client;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin.Security;

namespace WebForms
{
    public partial class CallApi : System.Web.UI.Page
    {
        protected string Payload;

        private void SaveTokens(AuthenticationProperties properties, TokenResponse message)
        {
            if (!string.IsNullOrEmpty(message.AccessToken))
            {
                properties.Dictionary["access_token"] = message.AccessToken;
            }

            if (!string.IsNullOrEmpty(message.IdentityToken))
            {
                properties.Dictionary["id_token"] = message.IdentityToken;
            }

            if (!string.IsNullOrEmpty(message.RefreshToken))
            {
                properties.Dictionary["refresh_token"] = message.RefreshToken;
            }

            if (!string.IsNullOrEmpty(message.TokenType))
            {
                properties.Dictionary["token_type"] = message.TokenType;
            }

            if (message.ExpiresIn != 0)
            {
                var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(message.ExpiresIn);
                properties.Dictionary["expires_at"] = expiresAt.ToString();
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            RegisterAsyncTask(new PageAsyncTask(ApiRequest));
        }

        private bool TokenIsExpired(AuthenticationProperties props)
        {
            var expirationProp = props.Dictionary["expires_at"];
            var expirationTime = DateTime.Parse(expirationProp);
            return expirationTime < DateTime.UtcNow;
        }

        internal static DiscoveryCache _discoveryCache = new DiscoveryCache(Urls.IdentityServer);

        private async Task<TokenResponse> RefreshToken(AuthenticationProperties props, ClaimsIdentity identity)
        {
            if (props.Dictionary.TryGetValue("refresh_token", out var refreshToken))
            {
                var disco = await _discoveryCache.GetAsync();
                var refreshRequest = new RefreshTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = "interactive.webforms.sample",
                    ClientSecret = "secret",
                    RefreshToken = refreshToken
                };
                var http = new HttpClient();
                var response = await http.RequestRefreshTokenAsync(refreshRequest);
                if (!response.IsError)
                {
                    SaveTokens(props, response);
                    Context.GetOwinContext().Authentication.SignIn(props, identity);
                    return response;
                }
                throw new Exception($"Failed to refresh tokens: {response.Error}");
            }
            throw new Exception("Attempted to refresh a token without a refresh token saved");
        }

        private async Task ApiRequest()
        {
            var authResult = await Context.GetOwinContext().Authentication.AuthenticateAsync("cookies");
            var props = authResult.Properties.Dictionary;
            if (props.TryGetValue("access_token", out var accessToken))
            {
                if (TokenIsExpired(authResult.Properties))
                {
                    var tokenResponse = await RefreshToken(authResult.Properties, authResult.Identity);
                    accessToken = tokenResponse.AccessToken;
                }
                var http = new HttpClient();
                http.SetBearerToken(accessToken);
                var response = await http.GetStringAsync(Urls.SampleOwinApi + "identity");
                var parsed = JsonDocument.Parse(response);

                Payload = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });
            }
        }
    }
}
