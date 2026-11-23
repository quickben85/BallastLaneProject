# Backend instructions

Follow the [repository instructions](../AGENTS.md), the [architecture](../docs/architecture.md), and the [owning user story](../user%20stories/README.md).

## File organization

Declare each class, record class, and interface in its own C# file. Name the file after the type, such as `ISignIn.cs` and `SignIn.cs`. This also applies to internal types, static helpers, and test fakes.

## Endpoints and use cases

- Design RESTful endpoints. Use plural nouns for collections and resource IDs for individual items. Use `GET` for reads, `POST` to create or submit, `PUT` to replace, `PATCH` for partial updates, and `DELETE` to remove a resource. Keep `GET` free of state changes.
- Use HTTP status codes to report the result. Return `201 Created` with a `Location` header when creating a resource that has its own URL. Document the response and expected errors in the owning story.
- Group Minimal API endpoints by resource. Require authorization on the task group. Declare public routes explicitly. Bind the request, obtain the trusted principal when needed, pass cancellation through, and map the use-case result to HTTP.
- Configure CORS from the explicit frontend origin allowlist and run it before authorization. Preserve bearer-token validation on protected routes.
- Derive the owner ID from the validated `sub` claim on every task operation. Treat owner ID, task ID on creation, and initial task status as server-owned. Apply ownership in the database query. Report missing and inaccessible items as the same `404`.
- Inject named application use cases into endpoints through interfaces defined in Application. Keep application inputs and results independent of HTTP types. Put task invariants in Domain and enforce them on every write. Keep expected validation failures and other expected outcomes explicit.

## Request validation

- Bind JSON request DTOs with `[FromBody]`. Handle body binding errors at the API host level so endpoints receive typed requests and malformed JSON gets a consistent `400` response.
- Use FluentValidation for request-specific checks on incoming DTOs. Define concrete `AbstractValidator<TRequest>` classes in the owning API host and register them as `IValidator<TRequest>`.
- In Minimal API handlers, call `ValidateAsync` with the request cancellation token before invoking the use case. Return field-keyed validation Problem Details using the JSON field names. See the [FluentValidation Minimal APIs guide](https://docs.fluentvalidation.net/en/latest/aspnet.html#minimal-apis).
- Define task invariants once in the Domain FluentValidation rules. The Domain factory calls those rules so every new task meets them. Task title length after trimming and the earliest allowed due date belong here. The API maps Domain errors to JSON fields.

## Repository and service interfaces

Inject every app-owned use case, repository, service, adapter, and validator through an interface. Use Application-defined interfaces across the Application boundary and internal interfaces for collaborators used only within Infrastructure. Register concrete implementations at startup and resolve dependencies by their interfaces, including when two interfaces share the same scoped implementation.

Call stateless static helpers directly. Entities and DTOs are concrete data types. Framework entry points, framework-provided dependencies such as Identity managers, and direct construction of the implementation under test are also allowed. Register API validators against `IValidator<TRequest>`.

### Repository example

Name repository interfaces `I<Entity>Repository` and their production implementations `<Entity>Repository`. Keep persistence library names out of the types and filenames.

Define `ITaskRepository` in Application. Keep its methods specific to the use case and include owner scope when operating on existing tasks. In this example, `TaskItem` is the Domain entity.

`ITaskRepository.cs`

```csharp
public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task<TaskItem?> FindForOwnerAsync(
        Guid taskId, Guid ownerId, CancellationToken cancellationToken);
}
```

`IGetTask.cs`

```csharp
public interface IGetTask
{
    Task<TaskItem?> ExecuteAsync(Guid taskId, Guid ownerId, CancellationToken cancellationToken);
}
```

`GetTask.cs`

```csharp
public sealed class GetTask(ITaskRepository tasks) : IGetTask
{
    public Task<TaskItem?> ExecuteAsync(
        Guid taskId, Guid ownerId, CancellationToken cancellationToken)
    {
        return tasks.FindForOwnerAsync(taskId, ownerId, cancellationToken);
    }
}
```

Implement `ITaskRepository` as `TaskRepository` in Infrastructure. Filter by task ID and owner ID in SQL and complete the write before returning success. Keep Dapper and Npgsql types inside the adapter. Register the repository in Infrastructure's `AddTaskPersistence` extension.

```csharp
services.AddScoped<ITaskRepository, TaskRepository>();
```

Call the extension and register the use case against its interface in the Tasks API composition root.

```csharp
builder.Services.AddTaskPersistence();
builder.Services.AddScoped<IGetTask, GetTask>();
```

### Service example

Define `IAccessTokenIssuer` in Application and inject it into the concrete `SignIn` use case. After credentials are verified, `SignIn` can request a token by passing the user's ID. The adapter owns signing and key configuration. Keep signing and Identity framework types inside the adapters.

`IAccessTokenIssuer.cs`

```csharp
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(Guid userId);
}
```

`IssuedAccessToken.cs`

```csharp
public sealed record IssuedAccessToken(string Value, int ExpiresInSeconds);
```

Register `SignIn` as the implementation of `ISignIn` in Accounts API and inject `ISignIn` into the login endpoint. An endpoint test can substitute `ISignIn` to isolate HTTP behavior. An Application unit test can construct `SignIn` with a fake issuer and assert that failed sign-in never requests a token.

```csharp
builder.Services.AddScoped<ISignIn, SignIn>();
builder.Services.AddAccountServices();
```

## Database

- Use Dapper parameter binding and Npgsql pooled connections. Open connections for the operation and dispose them promptly. Pass cancellation tokens to Dapper commands and use a transaction when multiple writes must succeed together.
- Infrastructure owns the separate Accounts and Tasks SQL migration sets. Add a new versioned DbUp script for schema changes. Verify that fresh databases and existing databases can migrate with their data intact.
- Keep demo account provisioning Development-only and opt-in. Use Identity with the documented demo credentials and a stable user ID. Re-running setup must preserve existing records.

## Test-driven development

Follow red–green–refactor for each behavior change. For task creation, first run a unit test that expects a whitespace-only title to be rejected. Observe the intended assertion failure, implement the title rule, then rerun the test before refactoring. Use a repository fake in an Application test to verify that invalid input never reaches `AddAsync`.

- Use xUnit for Domain rules and Application behavior. Construct use cases directly with fakes of their interfaces.
- Use `WebApplicationFactory` and isolated PostgreSQL databases for integration tests. Apply real migrations. Exercise HTTP status and body, ownership, and persistence across requests. For cross-host authentication, use a token from the real Accounts API with test-only signing keys.
- Run the focused tests first, then `dotnet build backend/TaskManager.slnx` and `dotnet test backend/TaskManager.slnx` from the repository root once the solution exists.
