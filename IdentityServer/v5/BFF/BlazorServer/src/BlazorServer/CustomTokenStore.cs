// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel.AspNetCore.AccessTokenManagement;

namespace BlazorServer
{
    public class CustomTokenStore : IUserAccessTokenStore
    {
        ConcurrentDictionary<string, UserAccessToken> _tokens = new ConcurrentDictionary<string, UserAccessToken>();

        public Task ClearTokenAsync(ClaimsPrincipal user, UserAccessTokenParameters parameters = null)
        {
            var sub = user.FindFirst("sub").Value;
            _tokens.TryRemove(sub, out _);
            return Task.CompletedTask;
        }

        public Task<UserAccessToken> GetTokenAsync(ClaimsPrincipal user, UserAccessTokenParameters parameters = null)
        {
            var sub = user.FindFirst("sub").Value;
            _tokens.TryGetValue(sub, out var value);
            return Task.FromResult(value);
        }

        public Task StoreTokenAsync(ClaimsPrincipal user, string accessToken, DateTimeOffset expiration, string refreshToken = null, UserAccessTokenParameters parameters = null)
        {
            var sub = user.FindFirst("sub").Value;
            var token = new UserAccessToken
            {
                AccessToken = accessToken,
                Expiration = expiration,
                RefreshToken = refreshToken
            };
            _tokens[sub] = token;
            return Task.CompletedTask;
        }
    }
}
