using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace OpenApi.Bff.OpenApi
{
    public class OpenApiDocumentCombiner(HttpClient client, IOptions<OpenApiDocumentCombinerOptions> o)
    {
        private static readonly OpenApiStreamReader OpenApiStreamReader = new OpenApiStreamReader();

        public async Task<FileStreamHttpResult> CombineDocuments(CancellationToken cancellationToken)
        {
            OpenApiDocument doc = new OpenApiDocument();

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
                var docToMerge = OpenApiStreamReader.Read(stream, out var diagnostic);

                foreach (var path in docToMerge.Paths ?? [])
                {
                    doc.Paths[source.LocalPath + path.Key] = path.Value;
                }

                foreach (var schema in docToMerge.Components.Schemas)
                {
                    doc.Components.Schemas[schema.Key] = schema.Value;
                }
                foreach (var response in docToMerge.Components.Responses)
                {
                    doc.Components.Responses[response.Key] = response.Value;
                }

                foreach (var parameter in docToMerge.Components.Parameters)
                {
                    doc.Components.Parameters[parameter.Key] = parameter.Value;
                }

                foreach (var example in docToMerge.Components.Examples)
                {
                    doc.Components.Examples[example.Key] = example.Value;
                }

                foreach (var requestBody in docToMerge.Components.RequestBodies)
                {
                    doc.Components.RequestBodies[requestBody.Key] = requestBody.Value;
                }

                foreach (var header in docToMerge.Components.Headers)
                {
                    doc.Components.Headers[header.Key] = header.Value;
                }

                //// We intentionally don't copy the security schemes. 
                //foreach (var securityScheme in docToMerge.Components.SecuritySchemes)
                //{
                //    doc.Components.SecuritySchemes[securityScheme.Key] = securityScheme.Value;
                //}

                foreach (var link in docToMerge.Components.Links)
                {
                    doc.Components.Links[link.Key] = link.Value;
                }

                foreach (var callback in docToMerge.Components.Callbacks)
                {
                    doc.Components.Callbacks[callback.Key] = callback.Value;
                }

            }

            var memoryStream = new MemoryStream();
            doc.Serialize(memoryStream, OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
            memoryStream.Position = 0;
        
            return TypedResults.Stream(memoryStream);
        }
    }
}
