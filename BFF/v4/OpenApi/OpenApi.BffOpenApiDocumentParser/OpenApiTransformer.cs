using System.Text;
using Microsoft.OpenApi;

namespace OpenApi.BffOpenApiDocumentParser;

public class OpenApiTransformer
{
    public static async Task TransformOpenApiDocumentForBff(Stream openApiDocumentStream, Stream outputStream,
        Uri serverUri, string localPath)
    {
        var result = await OpenApiDocument.LoadAsync(openApiDocumentStream);
        var doc = result.Document;

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
        await using var textWriter = new StreamWriter(responseBody);

        // The OpenAPI library's async calls are actually synchronous
        // which explodes the pipeline since ASP.NET Core
        // purposely denies synchronous calls that may lead to a deadlock
        //
        // Also, don't try to upgrade the OpenAPI library to 3.x, it is
        // not currently compatible with .NET 10 :\
        // https://github.com/dotnet/aspnetcore/issues/64317
        //
        // 💣 This will mess up your day.
        // Please, trust us -- ❤️Duende Team
        //
        // await doc.SerializeAsync(writer, OpenApiSpecVersion.OpenApi3_0);

        var json = await doc.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);
        await textWriter.WriteAsync(json);

        await responseBody.FlushAsync();
    }
}
