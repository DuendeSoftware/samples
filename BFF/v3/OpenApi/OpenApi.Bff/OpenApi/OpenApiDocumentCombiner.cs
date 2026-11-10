using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace OpenApi.Bff.OpenApi
{
    public class OpenApiDocumentCombiner(HttpClient client, IOptions<OpenApiDocumentCombinerOptions> o)
    {
        // Component types that are namespaced per downstream document.
        // Security schemes are intentionally not copied (and so are not renamed).
        private static readonly string[] NamespacedComponentTypes =
        [
            "schemas", "responses", "parameters", "examples", "requestBodies", "headers", "links", "callbacks", "pathItems"
        ];

        private const string RefPrefix = "#/components/";

        public async Task<FileStreamHttpResult> CombineDocuments(CancellationToken cancellationToken)
        {
            var paths = new JsonObject();
            var components = new JsonObject();
            foreach (var type in NamespacedComponentTypes)
            {
                components[type] = new JsonObject();
            }

            var usedPrefixes = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;

            foreach (var source in o.Value.Documents)
            {
                index++;
                await using var stream = await client.GetStreamAsync(source.DocumentUri, cancellationToken);
                var node = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken);
                if (node is not JsonObject docToMerge)
                {
                    continue;
                }

                var prefix = CreateUniquePrefix(source.LocalPath, index, usedPrefixes);

                // Rewrite all references so they point to the namespaced components.
                RewriteReferences(docToMerge, prefix);

                if (docToMerge["paths"] is JsonObject docPaths)
                {
                    foreach (var path in docPaths.ToArray())
                    {
                        docPaths.Remove(path.Key);
                        paths[source.LocalPath + path.Key] = path.Value;
                    }
                }

                if (docToMerge["components"] is JsonObject docComponents)
                {
                    foreach (var type in NamespacedComponentTypes)
                    {
                        if (docComponents[type] is not JsonObject items)
                        {
                            continue;
                        }

                        var target = (JsonObject)components[type]!;
                        foreach (var item in items.ToArray())
                        {
                            items.Remove(item.Key);
                            target[prefix + "_" + item.Key] = item.Value;
                        }
                    }
                }
            }

            var result = new JsonObject
            {
                ["openapi"] = "3.1.1",
                ["info"] = new JsonObject { ["title"] = "Combined API", ["version"] = "1.0.0" },
            };
            if (o.Value.ServerUri != null)
            {
                result["servers"] = new JsonArray(new JsonObject { ["url"] = o.Value.ServerUri.ToString() });
            }
            result["paths"] = paths;
            result["components"] = components;

            var memoryStream = new MemoryStream();
            await using (var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
            {
                result.WriteTo(jsonWriter);
            }
            memoryStream.Position = 0;
            return TypedResults.Stream(memoryStream);
        }

        private static string CreateUniquePrefix(string localPath, int index, HashSet<string> used)
        {
            // Component keys must match ^[a-zA-Z0-9.\-_]+$
            var prefix = Regex.Replace(localPath ?? "", "[^a-zA-Z0-9.\\-]+", "_").Trim('_');
            if (prefix.Length == 0)
            {
                prefix = "Doc" + index;
            }
            var candidate = prefix;
            var n = 2;
            while (!used.Add(candidate))
            {
                candidate = prefix + "_" + n++;
            }
            return candidate;
        }

        private static void RewriteReferences(JsonNode? node, string prefix)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var property in obj.ToArray())
                    {
                        if ((property.Key == "$ref") && property.Value is JsonValue v
                            && v.TryGetValue<string>(out var reference))
                        {
                            obj[property.Key] = RewriteReference(reference, prefix);
                        }
                        else if (property.Key == "mapping" && property.Value is JsonObject mapping)
                        {
                            // Discriminator mappings may hold schema references as plain strings.
                            foreach (var m in mapping.ToArray())
                            {
                                if (m.Value is JsonValue mv && mv.TryGetValue<string>(out var target)
                                    && target.StartsWith(RefPrefix, StringComparison.Ordinal))
                                {
                                    mapping[m.Key] = RewriteReference(target, prefix);
                                }
                            }
                        }
                        else
                        {
                            RewriteReferences(property.Value, prefix);
                        }
                    }
                    break;
                case JsonArray array:
                    foreach (var item in array)
                    {
                        RewriteReferences(item, prefix);
                    }
                    break;
            }
        }

        private static string RewriteReference(string reference, string prefix)
        {
            if (!reference.StartsWith(RefPrefix, StringComparison.Ordinal))
            {
                return reference;
            }
            var rest = reference[RefPrefix.Length..];
            var slash = rest.IndexOf('/');
            if (slash < 0)
            {
                return reference;
            }
            var type = rest[..slash];
            if (!NamespacedComponentTypes.Contains(type))
            {
                return reference;
            }
            // Rest of the pointer after the component name (e.g. /properties/x) is preserved.
            var after = rest[(slash + 1)..];
            return $"{RefPrefix}{type}/{prefix}_{after}";
        }
    }
}
