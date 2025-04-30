namespace OpenApi.Bff.OpenApi;

public class OpenApiDocumentCombinerOptions
{
    public Uri? ServerUri { get; set; }

    public OpenApiDocumentSource[]  Documents { get; set; } = Array.Empty<OpenApiDocumentSource>();
}