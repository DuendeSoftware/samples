using Duende.IdentityModel.Client;

namespace CIMD.IdentityServer;

/// <summary>
/// Represents a Client ID Metadata Document (CIMD). Structurally identical to
/// <see cref="DynamicClientRegistrationDocument"/> — CIMD reuses the same JSON
/// schema as RFC 7591 Dynamic Client Registration, but the document is hosted
/// at a URL that serves as the client_id rather than being submitted to a
/// registration endpoint.
/// </summary>
public class CimdDocument : DynamicClientRegistrationDocument;
