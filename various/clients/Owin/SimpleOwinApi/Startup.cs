using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Http;
using Client;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Jwt;
using Owin;

namespace SimpleApi
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseJwtBearerAuthentication(new JwtBearerAuthenticationOptions
            {
                AuthenticationType = "jwt",
                AuthenticationMode = AuthenticationMode.Active,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Urls.IdentityServer,
                    ValidAudience = Urls.IdentityServer + "/resources",
                    IssuerSigningKeyResolver = LoadKeys,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                },
            });

            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            app.UseWebApi(config);
        }

        internal static DiscoveryCache _discoveryCache = new DiscoveryCache(Urls.IdentityServer);

        private IEnumerable<SecurityKey> LoadKeys(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
        {
            var disco = _discoveryCache.GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (disco.IsError) { throw new Exception("Failed to retrieve discovery information - " + disco.Error); }

            var keys = disco.KeySet.Keys
                .Where(x => x.N != null && x.E != null)
                .Select(x =>
                {
                    var rsa = new RSAParameters
                    {
                        Exponent = Base64UrlEncoder.DecodeBytes(x.E),
                        Modulus = Base64UrlEncoder.DecodeBytes(x.N),
                    };

                    return new RsaSecurityKey(rsa)
                    {
                        KeyId = x.Kid
                    };
                });

            return keys;
        }
    }
}
