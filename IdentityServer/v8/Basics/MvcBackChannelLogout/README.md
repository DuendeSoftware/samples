# MVC Client with Back-Channel Logout Notifications sample

This sample shows how to use back-channel logout notifications.

### Key takeaways:

- how to implement the back-channel notification endpoint
- how to leverage events on the cookie handler to invalidate the user session

 Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## How to Run

1. Run the Aspire project
1. Open a browser tab to th `client` application at `https://localhost:44300/`.
1. Click on the 'Secure' tab. You will be redirected to the IdentityServer application hosted at `https://localhost:5001`.
1. Login with username `bob` and password `bob`.
1. You will be redirected back to the `client` application's `/secure` page.
