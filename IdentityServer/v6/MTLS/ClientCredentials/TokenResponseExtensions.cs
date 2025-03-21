// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using IdentityModel.Client;

namespace Shared;

public static class TokenResponseExtensions
{
    public static void Show(this TokenResponse response)
    {
        if (!response.IsError)
        {
            "Token response:".ConsoleGreen();
            Console.WriteLine(response.Json);

            if (response.AccessToken.Contains("."))
            {
                "\nAccess Token (decoded):".ConsoleGreen();

                response.AccessToken.ShowAccessToken();
            }
        }
        else
        {
            if (response.ErrorType == ResponseErrorType.Http)
            {
                "HTTP error: ".ConsoleGreen();
                Console.WriteLine(response.Error);
                "HTTP status code: ".ConsoleGreen();
                Console.WriteLine(response.HttpStatusCode);
            }
            else
            {
                "Protocol error response:".ConsoleGreen();
                Console.WriteLine(response.Raw);
            }
        }
    }
}
