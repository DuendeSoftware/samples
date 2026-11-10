using System.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace OpenApi.BffOpenApiDocumentParser;

public class OpenApiTransformer
{
    // Microsoft.OpenApi 2.x only reads JSON by default; register the YAML reader
    // so documents proxied from /openapi/{document}.yaml can be parsed as well.
    private static readonly OpenApiReaderSettings ReaderSettings = CreateReaderSettings();

    private static OpenApiReaderSettings CreateReaderSettings()
    {
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        return settings;
    }

    public static async Task TransformOpenApiDocumentForBff(Stream openApiDocumentStream, Stream outputStream,
        Uri serverUri, string localPath)
    {
        var result = await OpenApiDocument.LoadAsync(openApiDocumentStream, settings: ReaderSettings);
        var doc = result.Document ?? throw new InvalidOperationException(
            "Unable to read OpenAPI document: " +
            string.Join("; ", result.Diagnostic?.Errors.Select(e => e.Message) ?? []));

        // Make sure the server is actually the BFF, not the original urls.
        // All traffic is supposed to go through the bff.
        doc.Servers = [new OpenApiServer { Url = serverUri.ToString() }];

        // We remove the JWT security scheme, because the BFF changes the way
        // the auth works to Cookie only. Specifying the cookie in the openapi document
        // isn't useful.
        doc.Components?.SecuritySchemes?.Clear();

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
