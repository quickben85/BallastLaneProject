# Backend instructions

Follow the [repository instructions](../AGENTS.md), the [architecture](../docs/architecture.md), and the [owning user story](../user%20stories/README.md).

## Endpoints and use cases

- Group Minimal API endpoints by resource. Require authorization on the task group. Declare public routes explicitly. Bind the request, obtain the trusted principal when needed, pass cancellation through, and map the use-case result to HTTP.
- Derive the owner ID from the validated `sub` claim on every task operation. Treat owner ID, task ID on creation, and initial task status as server-owned. Apply ownership in the database query. Report missing and inaccessible items as the same `404`.
- Use concrete, named application use cases and keep application inputs and results independent of HTTP types. Put task invariants in Domain and enforce them on every write. Keep expected validation failures and other expected outcomes explicit.

## Request validation

- Use FluentValidation for incoming request DTOs. Define concrete `AbstractValidator<TRequest>` classes in the owning API host and register them as `IValidator<TRequest>`.
- In Minimal API handlers, call `ValidateAsync` with the request cancellation token before invoking the use case. Return field-keyed validation Problem Details using the JSON field names. See the [FluentValidation Minimal APIs guide](https://docs.fluentvalidation.net/en/latest/aspnet.html#minimal-apis).
- Keep business invariants in Domain so they apply through every entry point. For example, the Domain enforces title length after trimming, and the API maps a rejected title to the `title` field.

## Repository and service interfaces

Use an Application-defined interface for every repository and every injectable service a use case depends on. Inject the interfaces through the use case constructor. Infrastructure implements repository and service adapters. The API hosts register implementations at startup. Keep named use cases, entities, DTOs, and simple validators concrete.

### Repository example

Define `ITaskRepository` in Application. Keep its methods specific to the use case and include owner scope when operating on existing tasks. In this example, `TaskItem` is the Domain entity.

```csharp
public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task<TaskItem?> FindForOwnerAsync(
        Guid taskId, Guid ownerId, CancellationToken cancellationToken);
}

public sealed class GetTask(ITaskRepository tasks)
{
    public Task<TaskItem?> ExecuteAsync(
        Guid taskId, Guid ownerId, CancellationToken cancellationToken)
    {
        return tasks.FindForOwnerAsync(taskId, ownerId, cancellationToken);
    }
}
```

Implement `ITaskRepository` as `DapperTaskRepository` in Infrastructure. Filter by task ID and owner ID in SQL and complete the write before returning success. Keep Dapper and Npgsql types inside the adapter. Register the repository and the concrete use case in Tasks API.

```csharp
builder.Services.AddScoped<ITaskRepository, DapperTaskRepository>();
builder.Services.AddScoped<GetTask>();
```

### Service example

Define `IAccessTokenIssuer` in Application and inject it into the concrete `SignIn` use case. After credentials are verified, `SignIn` can request a token by passing the user's ID. The adapter owns signing and key configuration. Keep signing and Identity framework types inside the adapters.

```csharp
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(Guid userId);
}

public sealed record IssuedAccessToken(string Value, int ExpiresInSeconds);
```

Register the implementation in Accounts API. An Application unit test can supply a fake issuer and assert that failed sign-in never requests a token.

```csharp
builder.Services.AddScoped<IAccessTokenIssuer, RsaJwtAccessTokenIssuer>();
builder.Services.AddScoped<SignIn>();
```

## Database

- Use Dapper parameter binding and Npgsql pooled connections. Open connections for the operation and dispose them promptly. Pass cancellation tokens to Dapper commands and use a transaction when multiple writes must succeed together.
- Infrastructure owns the separate Accounts and Tasks SQL migration sets. Add a new versioned DbUp script for schema changes. Verify that fresh databases and existing databases can migrate with their data intact.
- Keep demo data Development-only and opt-in. Seed users through Identity with the documented demo credentials and a stable user ID. Seed the task examples for that ID. Re-running setup must preserve existing user changes and avoid duplicate examples.

## Test-driven development

Follow red–green–refactor for each behavior change. For task creation, first run a unit test that expects a whitespace-only title to be rejected. Observe the intended assertion failure, implement the title rule, then rerun the test before refactoring. Use a repository fake in an Application test to verify that invalid input never reaches `AddAsync`.

- Use xUnit for Domain rules and Application behavior. Construct use cases directly with fakes of their interfaces.
- Use `WebApplicationFactory` and isolated PostgreSQL databases for integration tests. Apply real migrations. Exercise HTTP status and body, ownership, and persistence across requests. For cross-host authentication, use a token from the real Accounts API with test-only signing keys.
- Run the focused tests first, then `dotnet build backend/TaskManager.slnx` and `dotnet test backend/TaskManager.slnx` from the repository root once the solution exists.
