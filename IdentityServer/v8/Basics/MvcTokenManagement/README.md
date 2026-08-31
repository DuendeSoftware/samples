# MVC Client with automatic Access Token Management sample

This sample shows how to use Duende.AccessTokenManagement to automatically manage access tokens.

The sample uses a special client in the sample IdentityServer with a short token lifetime (75 seconds). When repeating the API call, make sure you inspect the returned iat and exp claims to observer how the token is slides.

You can also turn on debug tracing to get more insights in the token management library.

### Key takeaways:

- use Duende.AccessTokenManagement to automate refreshing tokens


Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.
 
## How to Run

1. Run the Aspire project
1. Open a browser tab to th `client` application at `https://localhost:44300/`.
1. Click on the 'Secure' tab. You will be redirected to the IdentityServer application hosted at `https://localhost:5001`.
1. Login with username `bob` and password `bob`.
1. You will be redirected back to the `client` application's `/secure` page.
1. Click the `Call API` button to make a secure call to the Web API using the token provided by IdentityServer.
