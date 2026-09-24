// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

public class SubscriptionAuthMiddleware(JwtBearerValidator bearerValidator) : DefaultSocketSessionInterceptor
{
    private ConcurrentDictionary<ISocketSession, Connection> _connections = new();

    public override async ValueTask<ConnectionStatus> OnConnectAsync(ISocketSession session, IOperationMessagePayload message, CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = session.Connection.HttpContext;
            var bearerToken = httpContext.Request.Headers.Authorization;
            if (string.IsNullOrEmpty(bearerToken))
            {
                return ConnectionStatus.Accept();
            }

            var token = bearerToken.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
            var validatedToken = await bearerValidator.ValidateToken(token);

            if (validatedToken == null)
            {
                return ConnectionStatus.Accept();
            }
            //{
            //    return ConnectionStatus.Reject("provided token was invalid");
            //}

            CancellationTokenSource ct = new CancellationTokenSource();

            var validity = validatedToken.Value.Token.ValidTo - DateTime.UtcNow;

            if (validity < TimeSpan.Zero)
            {
                return ConnectionStatus.Reject("token no longer valid");
            }

            Console.WriteLine("Token valid for " + validity);

            _connections.TryAdd(session, new Connection()
            {
                TokenSource = ct,
                CancelTask = Task.Delay(validity)
                    .ContinueWith(async (_, __) =>
                    {
                        _connections.Remove(session, out var _);
                        await session.Connection.CloseAsync("Refresh", ConnectionCloseReason.NormalClosure,
                            ct.Token);
                    }, null, ct.Token)
            });

            return ConnectionStatus.Accept();
        }
        catch (Exception ex)
        {
            return ConnectionStatus.Reject(ex.Message);
        }
    }

    public override async ValueTask OnCloseAsync(ISocketSession session, CancellationToken cancellationToken = new CancellationToken())
    {
        if (_connections.TryRemove(session, out var connection))
        {
            await connection.TokenSource.CancelAsync();
            connection.Dispose();
        }
    }
}
