---
name: setup-local-app
description: Set up, start, and verify the Task Manager app on macOS, Windows, or Linux. Use for first-time local setup, signing keys and app settings, Docker Compose startup, or running PostgreSQL, ASP.NET Core, and Angular directly.
---

# Set up the local app

Use Docker Compose by default. Choose native mode when the user asks to run without Docker. Use the instructions for the detected operating system in the [README](../../../README.md#full-stack-setup). Perform setup and verification in the workspace and use the documented commands for a user who prefers to run them manually.

## Prepare keys and settings

1. Work from the repository root. Check for the .NET 10 SDK. For Compose, also check the Docker daemon and Compose plugin. For native mode, check Node.js 24 with npm, PostgreSQL 18, and the PostgreSQL client tools on PATH. If a prerequisite is unavailable, report the missing tool or service and continue any setup that can run independently.
2. Generate or verify the local RSA keys and fill in missing database passwords in the Git-ignored `.env` file.

   ```text
   dotnet run --file .agents/skills/setup-local-app/scripts/GenerateKeys.cs
   ```

   Accounts API uses `.local/accounts-private.pem` and Tasks API uses `.local/accounts-public.pem`. Keep existing keys and database credentials. Report invalid or mismatched keys without exposing their contents. Rotate keys only if requested.

## Start the chosen mode

### Docker Compose

1. Follow [Run with Docker Compose](../../../README.md#run-with-docker-compose). Keep the existing seed setting unless the user asks for demo sign-in. For a demo setup, set `DEMO_SEED=true` in `.env`.
2. Run `docker compose config --quiet`, then `docker compose up --build --wait`. Compose provisions PostgreSQL, mounts each key read-only in its API container, and starts both APIs and the static frontend.
3. Verify the four services with `docker compose ps`. Check both API liveness URLs and the frontend URL from the README. Use the configured `FRONTEND_PORT` if it differs from `8088`.

### Native

1. Follow [Run without Docker](../../../README.md#run-without-docker). Start the local PostgreSQL service if it is installed and stopped. Connect with an available administrator role. Check for both application roles and databases before provisioning. Create only the missing resources and reuse configured passwords. If administrative access or the credentials for an existing role are unavailable, ask the user for the missing setup step before changing that database.
2. Run the Accounts and Tasks commands for the detected shell in separate processes. Set each API's connection string, absolute key path, and frontend origin as documented. Apply the Accounts demo seed setting when the user asks for demo sign-in. Run `npm --prefix frontend ci` and start Angular in a third process. Keep the started processes available for the user.
3. Verify that both APIs report `Healthy` and that Angular serves the sign-in page. Use `4200` for the frontend or configure the exact alternative origin in both APIs.

When demo sign-in is enabled, verify it using the documented demo credentials. Report the mode, running URLs, verification results, and any remaining blocker. Keep private keys, passwords, and tokens out of the output.
