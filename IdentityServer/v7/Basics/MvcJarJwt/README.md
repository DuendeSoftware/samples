# MVC Client with JAR and JWT-based Authentication sample

This sample shows how to use signed authorize requests, and JWT-based authentication for clients in MVC. It also shows how to integrate that technique with automatic token management.

### Key takeaways:

- use the ASP.NET Core extensibility points to add signed authorize requests and JWT-based authentication
- use JWT-based authentication for automatic token management
- configure a client in IdentityServer to share key material for both front- and back-channel


Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## How to Run

1. Run the Aspire project
1. Open a browser tab to th `client` application at `https://localhost:44300/`.
1. Click on the 'Secure' tab. You will be redirected to the IdentityServer application hosted at `https://localhost:5001`.
1. Login with username `bob` and password `bob`.
1. You will be redirected back to the `client` application's `/secure` page.
1. Click the `Call API` button to make a secure call to the Web API using the token provided by IdentityServer.
