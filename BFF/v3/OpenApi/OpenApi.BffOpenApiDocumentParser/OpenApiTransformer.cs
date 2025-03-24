using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace OpenApi.BffOpenApiDocumentParser;

public class OpenApiTransformer
{
    private static readonly OpenApiStreamReader OpenApiStreamReader = new OpenApiStreamReader();

    public static async Task TransformOpenApiDocumentForBff(Stream openApiDocumentStream, Stream outputStream, Uri serverUri, string localPath)
    {
        var doc = OpenApiStreamReader.Read(openApiDocumentStream, out var diagnostic);

        // Make sure the server is actually the BFF, not the original urls. 
        // All traffic is supposed to go through the bff. 
        doc.Servers.Clear();
        doc.Servers.Add(new OpenApiServer()
        {
            Url = serverUri.ToString()
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
            doc.Paths[localPath + path.Key] = path.Value;
        }


        await WriteDocumentTo(doc, outputStream);
    }



    private static async Task WriteDocumentTo(OpenApiDocument doc, Stream responseBody)
    {
        var memoryStream = new MemoryStream();
        doc.Serialize(memoryStream, OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(responseBody);
        await responseBody.FlushAsync();
    }

}
