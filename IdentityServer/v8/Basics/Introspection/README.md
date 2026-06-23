# Introspection and reference tokens sample

This sample shows how to use the reference tokens instead of JWTs.

### Things of interest:

- the client registration uses AccessTokenType of value Reference
- the client requests scope2 - this scope is part of an API resource.
- API resources allow defining API secrets, which can then be used to access the introspection endpoint
- The API supports both JWT and reference tokens, this is achieved by forwarding the token to the right handler at runtime

### Key takeaways:

- configuring a client to receive reference tokens
- set up an API resource with an API secret
- configure an API to accept and validate reference tokens

 Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.
