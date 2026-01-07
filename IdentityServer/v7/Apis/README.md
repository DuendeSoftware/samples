# Sample APIs

These APIs are shared between the various samples.



Most samples use the *SimpleApi* which just accepts any token issued by the sample IdentityServer. The *IdentityController* then echoes back the claims to the client.

The *ResourceBasedApi* is a API that uses the *ApiResource* concept from Duende IdentityServer. It can make use of some advanced features like audience validation and token introspection.