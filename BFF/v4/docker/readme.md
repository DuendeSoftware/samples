# Development Certificate Setup (HTTPS in Docker)

This project uses Docker Compose and requires an ASP.NET Core development certificate (`.pfx` file) to enable HTTPS communication for the services running inside containers during local development. After cloning the repository, you will need to generate this file.

## Method Used

This setup uses the **`COPY`** method within the Dockerfiles (`ContainerizedIdentityServer/Dockerfile` and `FrontendHost/Dockerfile`). This means:

1.  You will generate a certificate file named `aspnetcore-dev-cert.pfx` in the root `docker` directory (the same directory as `compose.yaml`).
2.  This file is copied into the Docker images during the `docker compose build` process.
3.  The applications (Kestrel) load this certificate from within the container using a path and password specified in `compose.yaml`.

This method avoids common permission issues associated with volume mounting certificate files from the host machine, especially across different operating systems (macOS, Windows).

## Steps to Set Up the Certificate

Follow these steps in a terminal opened in the project's root `docker` directory (where `compose.yaml` is located).

**1. Generate/Export the Certificate:**

* You need to export the ASP.NET Core development certificate from your machine's certificate store into the required `.pfx` file.
* **Choose a Password:** Decide on a password for the exported `.pfx` file. You will need this password in the next step. We use `MyPw123` as an example here.
* **Run the Export Command:** Execute the following command in your terminal:
    ```powershell
    # Make sure you are in the 'docker' directory
    dotnet dev-certs https --export-path ./aspnetcore-dev-cert.pfx --password MyPw123 --format PFX
    ```
  * **Note:** While `MyPw123` is used as an example, you should replace it with a secure password of your choice. **Remember this password** for the next step.
  * This command finds the valid ASP.NET Core HTTPS development certificate on your system and exports it (including its private key) to the `aspnetcore-dev-cert.pfx` file in the current directory, protecting it with the password you provided.
* **Windows Permission Troubleshooting:**
  * If you encounter an "Access Denied" or "Permission denied" error when running this command on Windows, please refer to the steps outlined in the `windows_permission_issue` document (check folder/file security settings, Controlled Folder Access, try running as Administrator temporarily to diagnose).
  * You may also need to clean existing HTTPS development certificates first by running `dotnet dev-certs https --clean` (this might require administrator privileges) before the export command will succeed. You must be able to successfully export the certificate using your regular user account.
* **Trust the Certificate (Host):** Ensure your host machine trusts the ASP.NET Core development certificate authority so your *browser* doesn't show certificate warnings when accessing `https://localhost:...`. If you haven't done this before, run:
    ```powershell
    dotnet dev-certs https --trust
    ```
  (This may require administrator privileges).

**2. Update Password in `compose.yaml`:**

* This step ensures the applications running in Docker use the correct password to load the certificate you just exported.
* Open the `compose.yaml` file.
* Locate the `environment:` section for **both** the `containerizedidentityserver` and `frontendhost` services.
* Find the variable `ASPNETCORE_Kestrel__Certificates__Default__Password`.
* Update its value to match the **exact password** (e.g., `MyPw123` or your chosen password) you used in the `dotnet dev-certs --export-path` command.
    ```yaml
    services:
      containerizedidentityserver:
        # ... other settings ...
        environment:
          # ... other variables ...
          - ASPNETCORE_Kestrel__Certificates__Default__Password=MyPw123 # <-- UPDATE THIS to match export password
      frontendhost:
        # ... other settings ...
        environment:
          # ... other variables ...
          - ASPNETCORE_Kestrel__Certificates__Default__Password=MyPw123 # <-- UPDATE THIS to match export password
    ```

**3. Rebuild and Run:**

* After ensuring the `.pfx` file exists (from Step 1) and the password in `compose.yaml` is correct (from Step 2), rebuild your Docker images and run the containers:
    ```bash
    # Optional: Clean up old containers/volumes if troubleshooting
    # docker compose down -v

    # Build images and start containers
    docker compose up --build
    ```

By following these steps, you ensure that the correct certificate file is generated, included in the Docker images, and that the applications inside the containers have the correct password to load it for HTTPS.
