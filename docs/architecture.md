# Task Manager architecture

## Stack

| Area | Choice |
| --- | --- |
| Backend | .NET 10 LTS, C#, ASP.NET Core Minimal APIs |
| Application | Named use cases called by the API through Application-defined interfaces |
| Persistence | PostgreSQL with Npgsql, Dapper, and SQL migrations tracked by DbUp |
| Accounts | ASP.NET Core Identity with a PostgreSQL user store implemented using Dapper |
| Validation | FluentValidation for request checks in API hosts and task invariants in Domain |
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

- **Domain** owns the task entity and its invariants. It references FluentValidation to enforce task rules when creating entities. It has no project references.
- **Application** depends on Domain. It owns use cases, their inputs and results, and interfaces for outside dependencies such as `ITaskRepository`.
- **Infrastructure** depends on Application and Domain. It implements the dependency interfaces and owns Dapper queries, PostgreSQL connections, SQL migrations, and demo account provisioning. It implements the custom user store behind Identity's storage interfaces.
- **Tasks API and Accounts API** each reference Application and Infrastructure. They own routing, transport models, response mapping, authentication configuration, and dependency injection. The two hosts have no project references to each other.
- **Frontend** depends on the HTTP contracts. Angular owns pages, standalone components, state, forms, and API calls.

Define use-case, repository, and service interfaces in Application when they cross its boundary. Use internal interfaces for collaborators within Infrastructure and typed injection tokens for Angular service interfaces. Register concrete implementations at startup. Stateless static helpers can be called directly. Entities, DTOs, and framework-managed types use their concrete types where appropriate. See the [backend dependency examples](../backend/AGENTS.md#repository-and-service-interfaces).

## API boundaries

The V6 “second API” requirement maps to two independently runnable ASP.NET Core hosts. **Accounts API** owns user creation and login. **Tasks API** owns task CRUD and enforces user isolation. Angular calls each API directly at its configured origin. The APIs use an explicit CORS allowlist for the local frontend origins. See the [ASP.NET Core CORS guidance](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0).

Both APIs must expose RESTful endpoints. Model resources with resource-oriented URLs, use standard HTTP methods and status codes, and keep requests stateless. Use camel-case JSON and Problem Details. Route paths, methods, permissions, payloads, and status codes belong in the [owning user story](../user%20stories/README.md).

Both API hosts use shared exception handlers. Request binding failures return safe client errors. Unexpected request failures return generic HTTP 500 Problem Details with a trace ID. The Development exception fallback uses the same response. Server logs record the exception details with the same trace ID.

## Local container topology

Run each vertical slice against the shared Docker Compose stack, with one container per service.

| Service | Responsibility |
| --- | --- |
| PostgreSQL | Hosts the `accounts` and `tasks` databases on a project-scoped named volume |
| Accounts API | Runs the independent ASP.NET Core Accounts host on local port `5081` |
| Tasks API | Runs the independent ASP.NET Core Tasks host on local port `5082` |
| Frontend | Runs a minimal Node.js static server on local port `8088` with support for client-side route refreshes |

Compose publishes the frontend and both API ports on loopback. PostgreSQL is accessed over the Compose network. The Angular CLI dev server on port `4200` uses the same API origins as the packaged frontend. See [Docker port publishing](https://docs.docker.com/engine/network/port-publishing/).

Provision both databases before starting the API hosts. Use a PostgreSQL readiness check in Compose and apply migrations in each host before it begins serving traffic. Run one stack at a time with the documented API ports. See [Compose startup order](https://docs.docker.com/compose/how-tos/startup-order/).

## Authentication

Accounts API uses ASP.NET Core Identity for password hashing and verification. For the local exercise, it issues short-lived JWTs using an RSA private key and sets the immutable user ID as `sub`. Tasks API uses the public key to validate the algorithm, signature, issuer, audience, lifetime, and subject. It validates tokens locally and uses the authenticated subject as the owner for each task operation.

Angular keeps the token in memory and attaches it only to protected task requests. Reloading or session expiry requires sign-in. Signing out clears the token and task state. See the [ASP.NET Core JWT bearer guidance](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

## Persistence

Use one PostgreSQL instance with separate `accounts` and `tasks` databases. Accounts owns the user records. Tasks owns task records with a unique ID, owner ID, title, optional description, status, and an optional date-only due date. Tasks stores the owner's immutable user ID and never reads the accounts database.

Open pooled connections through Npgsql and use parameterized SQL through Dapper. Scope task queries and mutations by owner in SQL. Infrastructure keeps versioned SQL scripts and migration history separate for each database. Each API applies only the migrations for its database on Development startup. Integration tests use isolated PostgreSQL databases and apply the same migrations.

Repository names describe the entity they store. Application defines `ITaskRepository`, and Infrastructure implements it as `TaskRepository` using Dapper and Npgsql internally.

Accounts can provision a demo user through Identity with a stable user ID when explicitly enabled in Development. A new task list is empty until its owner creates a task.
