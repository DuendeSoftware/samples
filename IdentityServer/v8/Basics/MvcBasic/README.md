# MVC Client sample

This sample shows how to use the authorization_code grant type. This is typically used for interactive applications like web applications.

### Key takeaways:

- configure an MVC client to use IdentityServer
- access tokens in ASP.NET Core’s authentication session
- call an API
- manually refresh tokens


Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## How to Run

1. Run the Aspire project
1. Open a browser tab to th `client` application at `https://localhost:44300/`.
1. Click on the 'Secure' tab. You will be redirected to the IdentityServer application hosted at `https://localhost:5001`.
1. Login with username `bob` and password `bob`.
1. You will be redirected back to the `client` application's `/secure` page.
1. Click the `Call API` button to make a secure call to the Web API using the token provided by IdentityServer.
