# Task Manager

A .NET full-stack project for the Ballast Lane V6 technical interview exercise.

- [User-story template and implemented stories](user%20stories/README.md)
- [Architecture](docs/architecture.md)
- [Repository instructions](AGENTS.md)

## Codex and Claude Code

Open either tool at the repository root. Both use the shared repository instructions in [AGENTS.md](AGENTS.md) and the same workflows.

| Workflow | Codex | Claude Code |
| --- | --- | --- |
| Set up and verify the app | `$setup-local-app Docker Compose with demo sign-in` | `/setup-local-app Docker Compose with demo sign-in` |
| Implement a vertical slice | `$implement-use-case <use-case details>` | `/implement-use-case <use-case details>` |

The workflow files live in [.agents/skills](.agents/skills). The [.claude/skills](.claude/skills) entries load those same files and read `AGENTS.md` explicitly, including on Claude Code versions that predate automatic loading of `AGENTS.md`.

## Full-stack setup

### Run with Docker Compose

Install the .NET 10 SDK and Docker with the Compose plugin. Use Linux containers in Docker Desktop on Windows. Start Docker, open a terminal at the repository root, and check the prerequisites.

```text
dotnet --version
docker compose version
```

**1. Generate the signing keys and local database settings**

Use the [setup skill](#codex-and-claude-code) with “Docker Compose with demo sign-in” to automate and verify the full setup. For manual setup, generate the keys and settings with this command.

```text
dotnet run --file .agents/skills/setup-local-app/scripts/GenerateKeys.cs
```

The command writes the private key for Accounts API to `.local/accounts-private.pem` and the public key for Tasks API to `.local/accounts-public.pem`. It generates any missing database passwords in `.env` and preserves existing keys and credentials. Compose mounts the keys read-only and configures each API's key path and database connection.

**2. Enable the demo account**

Open the generated `.env` in the repository root. Add the following line or update its existing value, then save the file.

```dotenv
DEMO_SEED=true
```

**3. Build and start the stack**

```text
docker compose config --quiet
docker compose up --build --wait
docker compose ps
```

Compose starts `postgres`, `accounts-api`, `tasks-api`, and `frontend`. PostgreSQL creates both databases and their application logins on first startup. The APIs run their DbUp migrations before serving requests.

| Component | URL | What to check |
| --- | --- | --- |
| Angular | [http://localhost:8088](http://localhost:8088) | Sign-in page |
| Accounts API | [http://localhost:5081/_internal/live](http://localhost:5081/_internal/live) | `Healthy` |
| Tasks API | [http://localhost:5082/_internal/live](http://localhost:5082/_internal/live) | `Healthy` |

Sign in with the [demo credentials](#demo-sign-in). If startup fails, inspect the service output with `docker compose logs --tail=100 postgres accounts-api tasks-api frontend`. Set `FRONTEND_PORT` in `.env` to change the frontend port, then open the corresponding URL. Angular uses ports `5081` and `5082` for the APIs.

**4. Stop the stack**

```text
docker compose down
```

This keeps the named PostgreSQL volume. Reuse its `.env` credentials on later starts.

### Run without Docker

Install the .NET 10 SDK, Node.js 24 with npm, and a local [PostgreSQL 18 server and `psql` client](https://www.postgresql.org/download/). Add the PostgreSQL `bin` directory to your PATH if needed. Start your local PostgreSQL service on `127.0.0.1:5432`. If this project's Compose stack is running, stop it with `docker compose down` to free API ports `5081` and `5082`.

**1. Generate keys and provision PostgreSQL**

Use the [setup skill](#codex-and-claude-code) with “native setup with demo sign-in” to automate this workflow. For manual setup, run the .NET helper from the repository root and connect to local PostgreSQL.

```text
dotnet run --file .agents/skills/setup-local-app/scripts/GenerateKeys.cs
pg_isready -h 127.0.0.1 -p 5432
psql -h 127.0.0.1 -p 5432 -U postgres -d postgres
```

Use your local PostgreSQL administrator role in place of `postgres` if it has a different name. Enter its password if prompted. For a first-time setup, create the two application roles in `psql`.

```sql
CREATE ROLE accounts_app LOGIN;
CREATE ROLE tasks_app LOGIN;
```

Run each password command separately. At its prompts, enter the matching password from `.env` twice.

```text
\password accounts_app
\password tasks_app
```

Use `ACCOUNTS_DB_PASSWORD` for `accounts_app` and `TASKS_DB_PASSWORD` for `tasks_app`. Then create the databases and exit `psql`.

```sql
CREATE DATABASE accounts OWNER accounts_app;
CREATE DATABASE tasks OWNER tasks_app;
REVOKE CONNECT ON DATABASE accounts FROM PUBLIC;
REVOKE CONNECT ON DATABASE tasks FROM PUBLIC;
\q
```

On later runs, reuse those databases and their passwords.

**2. Start Accounts API in terminal 1**

Run your shell's block from the repository root. The password is read from the generated `KEY=value` entry in `.env`. You can also set `ACCOUNTS_DB_PASSWORD` in this terminal to use a password you already configured.

macOS or Linux shell

```sh
accounts_password=${ACCOUNTS_DB_PASSWORD:-$(sed -n 's/^ACCOUNTS_DB_PASSWORD=//p' .env | tail -n 1 | tr -d '\r')}
export ASPNETCORE_ENVIRONMENT=Development
export Auth__PrivateKeyPath="$PWD/.local/accounts-private.pem"
export ConnectionStrings__Accounts="Host=127.0.0.1;Port=5432;Database=accounts;Username=accounts_app;Password=$accounts_password"
export Frontend__Origins__0=http://localhost:4200
export Demo__Seed=true
dotnet run --project backend/src/TaskManager.Accounts.Api --no-launch-profile -- --urls http://127.0.0.1:5081
```

Windows PowerShell

```powershell
$accountsPassword = $env:ACCOUNTS_DB_PASSWORD
if (-not $accountsPassword) {
    $accountsPassword = (Get-Content .env | Where-Object { $_ -like 'ACCOUNTS_DB_PASSWORD=*' } | Select-Object -Last 1) -replace '^ACCOUNTS_DB_PASSWORD=', ''
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Auth__PrivateKeyPath = (Resolve-Path '.local/accounts-private.pem').Path
$env:ConnectionStrings__Accounts = "Host=127.0.0.1;Port=5432;Database=accounts;Username=accounts_app;Password=$accountsPassword"
$env:Frontend__Origins__0 = 'http://localhost:4200'
$env:Demo__Seed = 'true'
dotnet run --project backend/src/TaskManager.Accounts.Api --no-launch-profile -- --urls http://127.0.0.1:5081
```

**3. Start Tasks API in terminal 2**

Run your shell's block from the repository root. This uses the generated `TASKS_DB_PASSWORD` in `.env` or a value already set in this terminal.

macOS or Linux shell

```sh
tasks_password=${TASKS_DB_PASSWORD:-$(sed -n 's/^TASKS_DB_PASSWORD=//p' .env | tail -n 1 | tr -d '\r')}
export ASPNETCORE_ENVIRONMENT=Development
export Auth__PublicKeyPath="$PWD/.local/accounts-public.pem"
export ConnectionStrings__Tasks="Host=127.0.0.1;Port=5432;Database=tasks;Username=tasks_app;Password=$tasks_password"
export Frontend__Origins__0=http://localhost:4200
dotnet run --project backend/src/TaskManager.Tasks.Api --no-launch-profile -- --urls http://127.0.0.1:5082
```

Windows PowerShell

```powershell
$tasksPassword = $env:TASKS_DB_PASSWORD
if (-not $tasksPassword) {
    $tasksPassword = (Get-Content .env | Where-Object { $_ -like 'TASKS_DB_PASSWORD=*' } | Select-Object -Last 1) -replace '^TASKS_DB_PASSWORD=', ''
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Auth__PublicKeyPath = (Resolve-Path '.local/accounts-public.pem').Path
$env:ConnectionStrings__Tasks = "Host=127.0.0.1;Port=5432;Database=tasks;Username=tasks_app;Password=$tasksPassword"
$env:Frontend__Origins__0 = 'http://localhost:4200'
dotnet run --project backend/src/TaskManager.Tasks.Api --no-launch-profile -- --urls http://127.0.0.1:5082
```

Both APIs apply migrations in Development. Accounts creates the demo account with `Demo__Seed=true`. Check that [Accounts API](http://localhost:5081/_internal/live) and [Tasks API](http://localhost:5082/_internal/live) both respond with `Healthy`.

**4. Start Angular in terminal 3**

```text
npm --prefix frontend ci
npm --prefix frontend start
```

Open [http://localhost:4200](http://localhost:4200) and sign in with the [demo credentials](#demo-sign-in). Angular calls the APIs directly, and their CORS settings allow this frontend origin. To use a different frontend port, set `Frontend__Origins__0` to its exact URL in both API terminals before starting them, then pass the port to Angular, for example `npm --prefix frontend start -- --port 4210`.

Press Ctrl+C in each terminal to stop the frontend and APIs. PostgreSQL keeps the data.

## Demo sign-in

| Email | Password |
| --- | --- |
| `demo@example.test` | `DemoTasks!2026` |

Compose reads `DEMO_SEED=true` from `.env`. The native instructions set `Demo__Seed=true` in the Accounts API terminal. Sign in, enter a title, add an optional description or due date, and select **Create task**. The saved task appears in your private list. Use **Sign out** to return to sign-in.

To demonstrate registration, choose **Create an account** on the sign-in page. Register with an email and password, then sign in to see the new account's empty task list.

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

The full backend test suite requires Docker because Testcontainers starts an isolated PostgreSQL instance. To run only the backend unit tests without Docker, use this command.

```text
dotnet test backend/tests/TaskManager.UnitTests/TaskManager.UnitTests.csproj
```

## Planned interview walkthrough

1. Introduce the selected user story. Show the two API hosts and the dependency direction.
2. Demonstrate the public health check, then sign in with the demo account.
3. Show the create-and-list flow and the recorded red–green test for the title rule. Use the API integration test to demonstrate persistence and owner isolation in PostgreSQL.
4. Select **Edit** on a task, update its details, and save it as **Completed**. Edit it again and choose **Open** to reopen it.
5. Select **Delete** on a task and confirm it in the popup. Reload and sign in again to verify it stays gone.
