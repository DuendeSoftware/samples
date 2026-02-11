using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace OpenApi.Bff.OpenApi
{
    public class OpenApiDocumentCombiner(HttpClient client, IOptions<OpenApiDocumentCombinerOptions> o)
    {
        public async Task<FileStreamHttpResult> CombineDocuments(CancellationToken cancellationToken)
        {
            OpenApiDocument doc = new();

            if (o.Value.ServerUri != null)
            {
                doc.Servers.Add(new OpenApiServer()
                {
                    Url = o.Value.ServerUri.ToString()
                });
            }

            doc.Paths = new OpenApiPaths();
            doc.Components = new OpenApiComponents();

            foreach (var source in o.Value.Documents)
            {
                var stream = await client.GetStreamAsync(source.DocumentUri, cancellationToken);
                var result = await OpenApiDocument.LoadAsync(stream);
                var docToMerge = result.Document;

                foreach (var path in docToMerge.Paths ?? [])
                {
                    doc.Paths[source.LocalPath + path.Key] = path.Value;
                }

                doc.Components ??= new OpenApiComponents();
                doc.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
                doc.Components.Responses ??= new Dictionary<string, IOpenApiResponse>();
                doc.Components.Parameters ??= new Dictionary<string, IOpenApiParameter>();
                doc.Components.Examples ??= new Dictionary<string, IOpenApiExample>();
                doc.Components.RequestBodies ??= new Dictionary<string, IOpenApiRequestBody>();
                doc.Components.Headers ??= new Dictionary<string, IOpenApiHeader>();
                doc.Components.Links ??= new Dictionary<string, IOpenApiLink>();
                doc.Components.Callbacks ??= new Dictionary<string, IOpenApiCallback>();

                docToMerge.Components ??= new OpenApiComponents();

                foreach (var schema in docToMerge.Components.Schemas?.ToArray() ??[])
                {
                    doc.Components.Schemas[schema.Key] = schema.Value;
                }
                foreach (var response in docToMerge.Components.Responses?.ToArray() ??[])
                {
                    doc.Components.Responses[response.Key] = response.Value;
                }

                foreach (var parameter in docToMerge.Components.Parameters?.ToArray() ??[])
                {
                    doc.Components.Parameters[parameter.Key] = parameter.Value;
                }

                foreach (var example in docToMerge.Components.Examples?.ToArray() ??[])
                {
                    doc.Components.Examples[example.Key] = example.Value;
                }

                foreach (var requestBody in docToMerge.Components.RequestBodies?.ToArray() ??[])
                {
                    doc.Components.RequestBodies[requestBody.Key] = requestBody.Value;
                }

                foreach (var header in docToMerge.Components.Headers?.ToArray() ??[])
                {
                    doc.Components.Headers[header.Key] = header.Value;
                }

                //// We intentionally don't copy the security schemes.
                //foreach (var securityScheme in docToMerge.Components.SecuritySchemes)
                //{
                //    doc.Components.SecuritySchemes[securityScheme.Key] = securityScheme.Value;
                //}

                foreach (var link in docToMerge.Components.Links?.ToArray() ?? [] )
                {
                    doc.Components.Links[link.Key] = link.Value;
                }

                foreach (var callback in docToMerge.Components.Callbacks?.ToArray() ?? [])
                {
                    doc.Components.Callbacks[callback.Key] = callback.Value;
                }

            }

            var memoryStream = await WriteToMemoryStream(cancellationToken, doc);

            return TypedResults.Stream(memoryStream);
        }

        private static async Task<MemoryStream> WriteToMemoryStream(CancellationToken cancellationToken, OpenApiDocument doc)
        {
            var memoryStream = new MemoryStream();
            await using var textWriter = new StreamWriter(memoryStream, leaveOpen: true);
            OpenApiJsonWriter writer = new(textWriter);

            doc.SerializeAsV3(writer);
            await textWriter.FlushAsync(cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
