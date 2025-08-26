This sample shows the effect of API scope / API resource configuration on the resulting access token.

See the [documentation](https://docs.duendesoftware.com/identityserver/fundamentals/resources) for more information.

Relevant points

* inspect the scopes and resources configuration
* use the sample client to request various scopes and inspect the resulting access token - both the `aud` and `scope` claims are of particular interest here
* toggle `EmitStaticAudienceClaim` and `EmitScopesAsSpaceDelimitedStringInJwt` to experiment with the token layout
* feel free to change the configuration, make sure the output is expected
