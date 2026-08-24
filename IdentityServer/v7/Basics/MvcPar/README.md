# MVC Client with Pushed Authorization Requests sample

This sample shows how to use Pushed Authorization Requests (PAR).

### Key takeaways:

- how to enable PAR in the client configuration
- how to add support for PAR to the ASP.NET OIDC authentication handler. The main idea is to use the events in the handler to push the parameters before redirecting to the authorize endpoint, and then replace the parameters that would normally be sent in that redirect with the resulting request uri. See the ParOidcEvents.cs file for more details.

### This sample is only relevant if you’re using .NET 8 or lower.

.NET 9 and higher versions have support for PAR built-in, and the ASP.NET Core OIDC authentication handler will automatically use PAR when the authority supports it, based on the discovery metadata.


Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## How to Run

1. Run the Aspire project
1. Open a browser tab to th `client` application at `https://localhost:44300/`.
1. Click on the 'Secure' tab. You will be redirected to the IdentityServer application hosted at `https://localhost:5001`.
1. Login with username `bob` and password `bob`.
1. You will be redirected back to the `client` application's `/secure` page.
1. Click the `Call API` button to make a secure call to the Web API using the token provided by IdentityServer.
