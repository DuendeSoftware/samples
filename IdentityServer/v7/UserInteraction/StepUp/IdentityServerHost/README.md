
# Duende IdentityServer Step-Up Demo
This demo shows how to implement [Step-Up authentication](https://datatracker.ietf.org/doc/draft-ietf-oauth-step-up-authn-challenge)
with Duende IdentityServer.

## Overview of Projects
The Demo consists of 3 projects:
- IdentityServer is a token server implemented with Duende IdentityServer.
- Api is a protected resource that uses the IdentityServer as its authority
  and can make Step-Up responses when requests don't meet its authentication
  requirements.
- Client is a client application that uses IdentityServer to login and makes
  requests to the Api.

## Running the Demo
To run the demo, start all three projects and navigate to the Client application
at https://localhost:6001. From there, you can click on links to pages that will
trigger step up in various ways. For example, you could
- Click on the secure page to trigger login.
- Authenticate with user alice, password alice.
- Note that alice does not require MFA to log in.
- Click on the MFA page to make an API request that requires MFA.
- This will trigger step up for Alice, who should be shown a fake MFA page at
  IdentityServer before returning to the Client application.
- Finally, click on the Recent Auth page to make an API request that requires an
  authentication in the past minute. The page will show the age of the authentication.
- It may be necessary to refresh the page after a minute has passed to trigger
  step up.

From there, you can experiment with other interactions. You can go to the Recent
Auth with MFA page that has both authentication requirements, or try the user
bob, who always requires MFA.

## Project Details

### Api
The Api contains endpoints in StepUpController.cs that have different
requirements: MFA, Recent authentication, both, or neither. When requirements
aren't met, the API responds with the WWW-Authenticate header values, indicating
which authentication requirements are required for Step-Up. The WWW-Authenticate
header is set in Api\Authorization\StepUpHandler.cs.

### Client
The Client application includes pages that call these endpoints. When the pages
get a Step-Up response, they trigger a challenge to the IdentityServer host,
including either the acr_values or max_age parameter (or both), to tell
IdentityServer what form of authentication is required. This challenge is issued
by an Http handler defined in Client\StepUpHandler.cs.

### IdentityServer
IdentityServer determines which page in the UI to show the user when an
authorization request comes in using the AuthorizeInteractionResponseGenerator.
The default AuthorizeInteractionResponseGenerator already respects the max_age
parameter and will use it to determine if it should prompt the user to log in
again.

We extend the default generator to handle ACR values that indicate that MFA has
been requested in the StepUpInteractionResponseGenerator. See our documentation
on [custom pages](https://docs.duendesoftware.com/identityserver/ui/custom/)
for more details.

Additionally, the UI in IdentityServer has been customized to show messages
telling the user what is happening during Step-Up.
