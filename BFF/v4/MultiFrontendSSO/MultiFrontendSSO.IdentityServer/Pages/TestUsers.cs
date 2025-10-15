// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityServer;
using Duende.IdentityServer.Test;
using IdentityModel;

namespace IdentityServer;

public static class TestUsers
{
    public static List<TestUser> Users
    {
        get
        {
            var address = new
            {
                street_address = "4 Jersey St",
                locality = "Boston",
                postal_code = "MA 02215",
                country = "United States"
            };

            return
            [
                new TestUser
                {
                    SubjectId = "1",
                    Username = "alice",
                    Password = "alice",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Alice Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Alice"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Email, "alice@smith.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.Roles, "Sales"),
                        new Claim(JwtClaimTypes.WebSite, "https://alicesmith.com"),
                        new Claim(
                            JwtClaimTypes.Address,
                            JsonSerializer.Serialize(address),
                            IdentityServerConstants.ClaimValueTypes.Json)
                    }
                },

                new()
                {
                    SubjectId = "2",
                    Username = "bob",
                    Password = "bob",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Bob Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Bob"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Roles, "Sales"),
                        new Claim(JwtClaimTypes.Email, "bob@smith.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.WebSite, "http://bobsmith.com"),
                        new Claim(
                            JwtClaimTypes.Address,
                            JsonSerializer.Serialize(address),
                            IdentityServerConstants.ClaimValueTypes.Json)
                    }
                }
            ];
        }
    }
}
