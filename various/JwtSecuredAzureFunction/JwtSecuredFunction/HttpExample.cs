// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace JwtSecuredFunction;

public static class HttpExample
{
    [Function("HttpExample")]
    public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("HttpExample");
        logger.LogInformation("C# HTTP trigger function processed a request.");

        var response = req.CreateResponse(HttpStatusCode.OK);

        var caller = await Token.ValidateAsync(
            req.Headers,
            logger);

        if (caller == null)
        {
            response.WriteString("Hello anonymous!");
            return response;
        }

        await response.WriteAsJsonAsync(caller.Claims.Select(c => new { c.Type, c.Value }));
        return response;
    }
}
