---
name: generate-local-keys
description: Generate or verify the local RSA key pair that Accounts API and Tasks API mount in Docker Compose for Task Manager development.
---

# Generate local signing keys

Run from the Task Manager repository root with the .NET 10 SDK on Windows, macOS, or Linux.

```text
dotnet run --file .agents/skills/generate-local-keys/scripts/GenerateKeys.cs
```

The tool writes `.local/accounts-private.pem` for Accounts API and `.local/accounts-public.pem` for Tasks API. It preserves matching keys and can derive a missing public key from an existing private key.

If the tool reports invalid or mismatched keys, report the error and preserve both files. Rotate or replace keys only when the user requests it. Report the paths and outcome without printing private key contents. The [README](../../../README.md#full-stack-setup) covers starting Compose.
