# Task Manager

A .NET full-stack project for the Ballast Lane V6 technical interview exercise.

- [User stories and API contracts](user%20stories/README.md)
- [Architecture](docs/architecture.md)
- [Repository instructions](AGENTS.md)

## Full-stack scaffold

Install Docker with the Compose plugin and OpenSSL. From the repository root, generate local PostgreSQL passwords and start the [four-container stack](docs/architecture.md#local-container-topology).

```sh
if [ ! -f .env ]; then
  (umask 077
   printf 'POSTGRES_PASSWORD=%s\nACCOUNTS_DB_PASSWORD=%s\nTASKS_DB_PASSWORD=%s\n' \
     "$(openssl rand -hex 24)" "$(openssl rand -hex 24)" "$(openssl rand -hex 24)" > .env)
fi

docker compose up --build --wait
```

Open [the Angular app](http://localhost:8088). The frontend health check waits for Node and both ASP.NET Core hosts to respond. PostgreSQL initializes both databases with separate application logins. Each API applies its DbUp migrations before serving traffic. The current migrations create schemas for future slices.

Run `docker compose down` to stop the stack and keep the named PostgreSQL volume. Use the same `.env` with that volume. Set `FRONTEND_PORT` in `.env` to override the published port.

## Frontend iteration

Install Node.js 24 with npm. The frontend uses Angular 21 LTS with the Angular CLI, standalone components, and the CLI's Vitest runner.

From the repository root, install and run the Angular workspace.

```sh
npm --prefix frontend ci
npm --prefix frontend start
```

Open [the Angular dev server](http://localhost:4200). Its standalone development proxy targets locally run Accounts and Tasks hosts on ports `5081` and `5082`, respectively. Use the Compose URL for full-stack checks.

To reproduce the scaffold in an empty directory, use Angular CLI 21.

```sh
npx --yes --package @angular/cli@21 ng new task-manager --directory frontend --standalone --routing --style css --test-runner vitest --skip-git --ssr=false --package-manager npm --defaults
```

## Planned demo credentials

| Email | Password |
| --- | --- |
| `demo@example.test` | `DemoTasks!2026` |

The seed includes one open task, “Prepare the interview walkthrough,” and one completed task, “Review the V6 brief.”

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

The backend suite currently runs host startup smoke tests. Use Compose to verify PostgreSQL provisioning and migrations.

## Planned interview walkthrough

1. Introduce the [create-and-list story](user%20stories/create-and-list-tasks.md). Show the two API hosts and the dependency direction.
2. Demonstrate the [public health check](user%20stories/service-health.md), then sign in with the demo account and show its seeded tasks.
3. Show the create-and-list flow and the recorded red–green test for the title rule. Use the API integration test to demonstrate persistence and owner isolation in PostgreSQL.
4. Demonstrate updating and deleting a task, then register a second account and show its empty list.
