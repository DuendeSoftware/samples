# Duende User Management Password Registration Sample

This sample demonstrates using IdentityServer v8 with Duende User Management to register a new user and set a password.

## Registration Flow

When navigating to the password-registration sample the /Index page presents the user with 2 choices, Login or Register.

### Registration

To register, a user will enter their email address, which is then validated by emailing an OTP code and waiting for the user to enter it. After the user proves they own the email address by entering the OTP, they are signed in and redirected to the /SetPassword page to create a password. Once the password is set, the user is signed out and redirected to the /Login page to sign-in with email/password.

### Login

After user sign-in, the /Index page displays user claims and custom data from the user's Attributes stored by User Management. The attributes can be modified by clicking on the Manage Account button.

## Unique Attribute

The `UserAttributes.Email` attribute has the `.IsUnique` property set to `true`. This is required to ensure the password is tied to that email address.

## Running the Sample

### Prerequisites

- .NET 10 SDK
- Docker (for Mailpit email testing via Aspire)

### Start with Aspire

```bash
cd UserManagementPasswordRegistration.AppHost
dotnet run
```

This launches:

| Service | URL |
|---------|-----|
| UserManagementPasswordRegistration | `https://password-registration.dev.localhost:6010` |
| Mailpit UI | `http://mailpit-aspire.dev.localhost:8026` |
| Mailpit SMTP | `smtp://localhost:1026` |
| Aspire Dashboard | `https://aspire.dev.localhost:17031` |
