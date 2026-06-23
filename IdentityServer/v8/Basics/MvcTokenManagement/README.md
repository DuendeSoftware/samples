# MVC Client with automatic Access Token Management sample

This sample shows how to use Duende.AccessTokenManagement to automatically manage access tokens.

The sample uses a special client in the sample IdentityServer with a short token lifetime (75 seconds). When repeating the API call, make sure you inspect the returned iat and exp claims to observer how the token is slides.

You can also turn on debug tracing to get more insights in the token management library.

### Key takeaways:

- use Duende.AccessTokenManagement to automate refreshing tokens


 Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.
