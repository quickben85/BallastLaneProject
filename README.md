# Task Manager

A .NET full-stack project for the Ballast Lane V6 technical interview exercise.

- [User stories and API contracts](user%20stories/README.md)
- [Architecture](docs/architecture.md)
- [Repository instructions](AGENTS.md)

## Full-stack setup

Install the .NET 10 SDK and Docker with the Compose plugin. Use Linux containers in Docker Desktop on Windows. The same commands work from PowerShell, Command Prompt, or a macOS or Linux terminal.

If this is your first run, create a plain-text `.env` file at the repository root. Replace each placeholder with a different random alphanumeric password. Keep your existing `.env` if you already initialized the PostgreSQL volume.

```dotenv
POSTGRES_PASSWORD=replace_with_random_password
ACCOUNTS_DB_PASSWORD=replace_with_another_random_password
TASKS_DB_PASSWORD=replace_with_a_third_random_password
DEMO_SEED=true
```

From the repository root, generate the RSA key pair and start the [four-container stack](docs/architecture.md#local-container-topology).

```text
dotnet run --file .agents/skills/generate-local-keys/scripts/GenerateKeys.cs
docker compose up --build --wait
```

In Codex, use [`$generate-local-keys`](.agents/skills/generate-local-keys/SKILL.md) to run the same key-generation step. The command preserves a matching key pair and recreates a missing public key from the existing private key. The `.env` file and `.local/` directory are Git-ignored.

Open [the Angular app](http://localhost:8088). Node serves the Angular files. The browser calls Accounts API on `http://localhost:5081` and Tasks API on `http://localhost:5082`. Compose publishes these ports on loopback, and the APIs allow browser requests from the configured frontend origins.

The frontend health check waits for Node and both API hosts to respond. PostgreSQL initializes both databases with separate application logins. Each API applies its DbUp migrations before serving traffic. Compose mounts only the signing key each API needs. The generator restricts host access to `.local/`. The non-root Accounts container reads its key through a read-only mount.

Run `docker compose down` to stop the stack and keep the named PostgreSQL volume. Use the same `.env` with that volume. Set `FRONTEND_PORT` in `.env` to override the published port.

## Frontend iteration

Install Node.js 24 with npm. The frontend uses Angular 21 LTS with the Angular CLI, standalone components, and the CLI's Vitest runner.

From the repository root, install and run the Angular workspace.

```sh
npm --prefix frontend ci
npm --prefix frontend start
```

Open [the Angular dev server](http://localhost:4200) while the API containers are running. The dev server uses the same direct API URLs. Compose allows this frontend origin alongside the packaged frontend.

To reproduce the scaffold in an empty directory, use Angular CLI 21.

```sh
npx --yes --package @angular/cli@21 ng new task-manager --directory frontend --standalone --routing --style css --test-runner vitest --skip-git --ssr=false --package-manager npm --defaults
```

## Demo sign-in

| Email | Password |
| --- | --- |
| `demo@example.test` | `DemoTasks!2026` |

Set `DEMO_SEED=true` in `.env` to enable the Development-only seed. Sign in to open the task page, then use **Sign out** to return to the form. Task CRUD and task seeding are planned for later slices.

## Build and test

```sh
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
```

With the .NET 10 SDK installed, run the backend checks from the repository root.

```sh
dotnet build backend/TaskManager.slnx
dotnet test backend/TaskManager.slnx
```

Run Docker for the backend integration tests. Testcontainers starts an isolated PostgreSQL instance and applies the Accounts migrations.

## Planned interview walkthrough

1. Introduce the [create-and-list story](user%20stories/create-and-list-tasks.md). Show the two API hosts and the dependency direction.
2. Demonstrate the [public health check](user%20stories/service-health.md), then sign in with the demo account and show its seeded tasks.
3. Show the create-and-list flow and the recorded red–green test for the title rule. Use the API integration test to demonstrate persistence and owner isolation in PostgreSQL.
4. Demonstrate updating and deleting a task, then register a second account and show its empty list.
