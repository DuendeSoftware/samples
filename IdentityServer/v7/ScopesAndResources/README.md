# Scopes And Resources sample

This sample shows the effect of API scope / API resource configuration on the resulting access token.

## How to Run this Sample

1. Run the Aspire project.
2. The Client project will have already completed running. Navigate to its console output.
3. Choose a Scope and Resource option from the client's output (ie a, b, c, etc).
4. Navigate to the parameters screen and change the value of the `resource` parameter to the option you chose.
5. Run the client project again and read its output in the console output.

See the [documentation](https://docs.duendesoftware.com/identityserver/fundamentals/resources) for more information.

## Relevant points

* inspect the scopes and resources configuration
* use the sample client to request various scopes and inspect the resulting access token - both the `aud` and `scope` claims are of particular interest here
* toggle `EmitStaticAudienceClaim` and `EmitScopesAsSpaceDelimitedStringInJwt` to experiment with the token layout
* feel free to change the configuration, make sure the output is expected
