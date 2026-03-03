# mTLS Sample

This sample shows how to use mTLS to bind tokens to a client. To run, follow these steps:

1. Generate a client certificate. This sample works well with the mkcert utility, which is a cross-platform tool for creating development certificates simply. 
2. Go to https://github.com/FiloSottile/mkcert and follow the installation instructions for your platform.
3. Run `mkcert -install` to generate and install a local CA.
4. Run `mkcert -client -pkcs12 localhost` from this directory to generate a client certificate.
4. Start up the IdentityServer and API projects.
5. Run the ClientCredentials project.
