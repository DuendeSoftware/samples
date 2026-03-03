# Session Management Sample

This sample requires all three projects to be run at once.

Things of note:
* In the *IdentityServer* project in *Startup.cs*, server-side sessions are enabled with a call to *AddServerSideSessions*. This only uses in-memory server-side sessions by default, so restarting the host will lose session data.
*  Also in *Startup.cs* with the call to *AddIdentityServer* various settings are configured on the *ServerSideSessions* options object to control the behavior.
* The client application configured in *Clients.cs* has *CoordinateLifetimeWithUserSession* enabled, which causes its refresh token to slide the server-side session for the user.
* When launching the *IdentityServer* project, you should visit the *~/serversidesessions* page to see the active sessions. Note that there is no authorization on this page (so consider adding it based on your requirements).
* Once you login, you should see a user's session in the list.
* As the client app refreshes its access token, you should see the user's session expiration being extended.
* When you revoke the user's session, the user should be logged out of the client app.