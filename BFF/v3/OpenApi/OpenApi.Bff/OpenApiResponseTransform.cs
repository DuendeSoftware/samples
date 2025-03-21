// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Yarp.ReverseProxy.Transforms;

namespace OpenApi.Bff;

/// <summary>
/// Transform the openapi document as it's being streamed. 
/// </summary>
/// <param name="basePath"></param>
public class OpenApiResponseTransform(string basePath) : ResponseTransform
{
    private static readonly OpenApiStreamReader OpenApiStreamReader = new OpenApiStreamReader();

    public override async ValueTask ApplyAsync(ResponseTransformContext context)
    {
        // Check if the request path matches /openapi/{document}.json / .yaml
        if (ProxyingOpenApiDocument(context))
        {
            if (context.ProxyResponse == null)
                // nothing to do if no response from the proxy
                return;

            var openApiDocumentStream = await context.ProxyResponse.Content.ReadAsStreamAsync();
            var doc = OpenApiStreamReader.Read(openApiDocumentStream, out var diagnostic);

            // This line is needed because we're going to modify the output stream. 
            // If we don't do this, it's going to send both the original and the modified stream.
            context.SuppressResponseBody = true;

            // Make sure the server is actually the BFF, not the original urls. 
            // All traffic is supposed to go through the bff. 
            doc.Servers.Clear();
            doc.Servers.Add(new OpenApiServer()
            {
                //Url = new Uri(Services.Bff.ActualUri(), basePath).ToString()
                Url = Services.Bff.ActualUri().ToString()
            });

            // We remove the JWT security scheme, because the BFF changes the way 
            // the auth works to Cookie only. Specifying the cookie in the openapi document
            // isn't useful. 
            doc.Components.SecuritySchemes.Clear();

            // Combine the pathbase with the paths in the document, so that the 
            // paths are correct for the proxy. 
            var allPaths = doc.Paths.ToArray();
            doc.Paths.Clear();
            foreach (var path in allPaths)
            {
                doc.Paths[basePath + path.Key] = path.Value;
            }

            await WriteDocumentToResponseStream(context, doc);
        }
    }

    private static async Task WriteDocumentToResponseStream(ResponseTransformContext context, OpenApiDocument doc)
    {
        var memoryStream = new MemoryStream();
        doc.Serialize(memoryStream, OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(context.HttpContext.Response.Body);
        await context.HttpContext.Response.Body.FlushAsync();
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
