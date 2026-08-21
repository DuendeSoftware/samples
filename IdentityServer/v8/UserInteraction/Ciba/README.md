**CIBA Sample**  

This sample requires multiple projects to be run at once. That can easily be done by running the included Aspire AppHost. The Aspire dashboard will show the status of all running applications and show you the links to the running applications. Aspire will also collect the Open Telemetry data (logs, metrics, traces) and make it available on the dashboard.  

## How to Run

1. Run the Aspire project.
1. Notice the `client` console application is already running. View its console output to see it is waiting for a user to allow the CIBA request.
1. Ensure you are logged in to user `alice` and then navigate to `https://localhost:5001/ciba/all`.
1. Click the `Process` button for the CIBA request and allow it.
1. Look at the console output again for the `client` application. Notice it continued processing after the CIBA request was authorized.

## More Information

For the included console application please look at the output of your IDE to see the results.

Please read the instructions for this sample [here](https://docs.duendesoftware.com/identityserver/samples/ui/#client-initiated-backchannel-login-ciba "https://docs.duendesoftware.com/identityserver/samples/ui/#client-initiated-backchannel-login-ciba").

