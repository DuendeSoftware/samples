// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.OpenApi.Models;
using OpenApi.BffOpenApiDocumentParser;
using Yarp.ReverseProxy.Transforms;

namespace OpenApi.Bff;

/// <summary>
/// TransformOpenApiDocumentForBff the openapi document as it's being streamed. 
/// </summary>
/// <param name="basePath"></param>
public class OpenApiResponseTransform(string basePath) : ResponseTransform
{

    public override async ValueTask ApplyAsync(ResponseTransformContext context)
    {
        // Check if the request path matches /openapi/{document}.json / .yaml
        if (ProxyingOpenApiDocument(context))
        {
            if (context.ProxyResponse == null)
                // nothing to do if no response from the proxy
                return;
            var outputStream = context.HttpContext.Response.Body;
            context.SuppressResponseBody = true;

            var openApiDocumentStream = await context.ProxyResponse.Content.ReadAsStreamAsync();
            await OpenApiTransformer.TransformOpenApiDocumentForBff(openApiDocumentStream, outputStream, Services.Bff.ActualUri(), basePath);
        }
    }


    private void AddPathBaseToAllPaths(OpenApiDocument doc)
    {
        var pathClones = doc.Paths.ToArray();
        doc.Paths.Clear();

        foreach (var path in pathClones)
        {
            doc.Paths[basePath + path.Key] = path.Value;
        }
    }

    private bool ProxyingOpenApiDocument(ResponseTransformContext context)
    {
        return context.HttpContext.Request.Path.StartsWithSegments(basePath +"/openapi", out var remainingPath) &&
               remainingPath.HasValue && (remainingPath.Value.EndsWith(".json") || remainingPath.Value.EndsWith(".yaml"));
    }
}
