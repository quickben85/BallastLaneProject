# Task Manager architecture

## Stack

| Area | Choice |
| --- | --- |
| Backend | .NET 10 LTS, C#, ASP.NET Core Minimal APIs |
| Application | Concrete use cases called directly by the API |
| Persistence | PostgreSQL with Npgsql, Dapper, and SQL migrations tracked by DbUp |
| Accounts | ASP.NET Core Identity with a PostgreSQL user store implemented using Dapper |
| Request validation | FluentValidation in the ASP.NET Core API hosts |
| Frontend | Angular 21 LTS, Angular CLI, standalone components, TypeScript, and responsive CSS |
| Local runtime | Docker Compose with PostgreSQL, two ASP.NET Core hosts, and a minimal Node.js frontend server |
| Tests | xUnit and WebApplicationFactory for the backend, Angular TestBed and CLI-managed Vitest for the frontend |

Angular 21 was the latest major in the Angular LTS phase when selected on November 21, 2026. See the [Angular release schedule](https://angular.dev/reference/releases#actively-supported-versions) and the [Angular CLI testing guide](https://angular.dev/guide/testing).

## Projects and dependencies

```text
backend/
  src/
    TaskManager.Domain/
    TaskManager.Application/
    TaskManager.Infrastructure/
    TaskManager.Tasks.Api/
    TaskManager.Accounts.Api/
  tests/
    TaskManager.UnitTests/
    TaskManager.IntegrationTests/
frontend/
  src/app/
    api/
    features/
    pages/
user stories/
```

- **Domain** owns the task entity and its invariants. It has no project references or framework dependencies.
- **Application** depends on Domain. It owns use cases, their inputs and results, and interfaces for outside dependencies such as `ITaskRepository`.
- **Infrastructure** depends on Application and Domain. It implements the dependency interfaces and owns Dapper queries, PostgreSQL connections, SQL migrations, and seed routines. It implements the custom user store behind Identity's storage interfaces.
- **Tasks API and Accounts API** each reference Application and Infrastructure. They own routing, transport models, response mapping, authentication configuration, and dependency injection. The two hosts have no project references to each other.
- **Frontend** depends on the HTTP contracts. Angular owns pages, standalone components, state, forms, and API calls.

Define repository and service dependency interfaces in Application. Inject those interfaces into use cases and register the implementations in each host. Keep named use cases, entities, DTOs, and simple validators concrete. See the [backend dependency examples](../backend/AGENTS.md#repository-and-service-interfaces).

## API boundaries

The V6 “second API” requirement maps to two independently runnable ASP.NET Core hosts. **Accounts API** owns user creation and login. **Tasks API** owns task CRUD and enforces user isolation. Angular calls both APIs through same-origin relative URLs.

Use camel-case JSON and Problem Details across both APIs. Route paths, methods, permissions, payloads, and status codes belong in the [user stories](../user%20stories/README.md).

## Local container topology

Run each vertical slice against the shared Docker Compose stack, with one container per service.

| Service | Responsibility |
| --- | --- |
| PostgreSQL | Hosts the `accounts` and `tasks` databases on a project-scoped named volume |
| Accounts API | Runs the independent ASP.NET Core Accounts host |
| Tasks API | Runs the independent ASP.NET Core Tasks host |
| Frontend | Runs a minimal Node.js server that serves the built Angular app, supports client-side route refreshes, and proxies API requests |

The frontend is the browser entry point. Node proxies requests to the correct API over the Compose network using service names. API responses retain their status, body, and contract headers. The Angular CLI dev server remains available for frontend iteration. See [Compose networking](https://docs.docker.com/compose/how-tos/networking/).

Provision both databases before starting the API hosts. Use a PostgreSQL readiness check in Compose and apply migrations in each host before it begins serving traffic. Use independent Compose project names when a slice needs an isolated stack. See [Compose startup order](https://docs.docker.com/compose/how-tos/startup-order/).

## Authentication

Accounts API uses ASP.NET Core Identity for password hashing, validation, and sign-in lockout. For the local exercise, it issues short-lived JWTs using an RSA private key and sets the immutable user ID as `sub`. Tasks API uses the public key to validate the algorithm, signature, issuer, audience, lifetime, and subject. It validates tokens locally and uses the authenticated subject as the owner for each task operation.

Angular keeps the token in memory and attaches it only to protected task requests. Reloading or session expiry requires sign-in. Signing out clears the token and task state. See the [ASP.NET Core JWT bearer guidance](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

## Persistence

Use one PostgreSQL instance with separate `accounts` and `tasks` databases. Accounts owns the user records. Tasks owns task records with a unique ID, owner ID, title, optional description, and status. Tasks stores the owner's immutable user ID and never reads the accounts database.

Open pooled connections through Npgsql and use parameterized SQL through Dapper. Scope task queries and mutations by owner in SQL. Infrastructure keeps versioned SQL scripts and migration history separate for each database. Each API applies only the migrations for its database on Development startup. Integration tests use isolated PostgreSQL databases and apply the same migrations.

Development demo seeding is opt-in. Accounts seeds a user through Identity with a stable user ID, and Tasks seeds sample records for that ID once per tasks database. Record seed completion in the same transaction as the task inserts to preserve edits and deletions across subsequent runs.
