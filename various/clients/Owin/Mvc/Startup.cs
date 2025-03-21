using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

[assembly: OwinStartup(typeof(OwinMvc.Startup))]

namespace OwinMvc
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                AuthenticationType = "cookies",
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                SlidingExpiration = true
            });

            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {

                AuthenticationType = "oidc",
                SignInAsAuthenticationType = "cookies",

                Authority = Urls.IdentityServer,

                ClientId = "interactive.mvc.owin.sample",
                ClientSecret = "secret",

                RedirectUri = "https://localhost:44301/",
                PostLogoutRedirectUri = "https://localhost:44301/",

                ResponseType = "code",
                Scope = "openid profile scope1 offline_access",

                UseTokenLifetime = false,
                SaveTokens = true,
                RedeemCode = true,
                UsePkce = true,
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = OnRedirectToIdentityProviderActions,
                }
            });

            app.UseStageMarker(PipelineStage.Authenticate);
        }

        private async Task OnRedirectToIdentityProviderActions(
            RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            await SetIdTokenHintOnLogout(notification);
            await ForbidInsteadOfChallengeIfAuthenticated(notification);
        }

        // Set the id_token_hint parameter during logout so that
        // IdentityServer can safely redirect back here after
        // logout. Unlike .NET Core authentication handler, the Owin
        // middleware doesn't do this automatically.
        private async Task SetIdTokenHintOnLogout(
            RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            if (notification.ProtocolMessage.PostLogoutRedirectUri != null)
            {
                var auth = await notification.OwinContext.Authentication.AuthenticateAsync("cookies");
                if (auth.Properties.Dictionary.TryGetValue("id_token", out var idToken))
                {
                    notification.ProtocolMessage.IdTokenHint = idToken;
                }
            }
        }

        // Do not challenge if the user is already authenticated, otherwise you get an inifinte loop on authorization failure
        private async Task ForbidInsteadOfChallengeIfAuthenticated(
            RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            if (notification.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication &&
               notification.OwinContext.Authentication.User.Identity.IsAuthenticated)
            {
                notification.HandleResponse();
                notification.OwinContext.Response.Redirect("/home/forbidden");
            }
        }
    }
}
