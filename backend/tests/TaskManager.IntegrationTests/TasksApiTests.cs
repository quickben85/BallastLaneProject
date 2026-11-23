using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace TaskManager.IntegrationTests;

public sealed class TasksApiTests(LoginFixture fixture) : IClassFixture<LoginFixture>
{
    [Fact]
    public async Task CreatesPersistsAndReturnsTaskOnlyToItsOwner()
    {
        const string otherEmail = "other-task-owner@example.test";
        const string otherPassword = "NextOwner!2026";
        var otherId = Guid.NewGuid();
        await using (var scope = fixture.Accounts.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var account = new IdentityUser<Guid> { Id = otherId, UserName = otherEmail };
            Assert.True((await users.CreateAsync(account, otherPassword)).Succeeded);
        }

        using var owner = fixture.Tasks.CreateClient();
        await AuthenticateAsync(owner);
        using var other = fixture.Tasks.CreateClient();
        await AuthenticateAsync(other, otherEmail, otherPassword);

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        using var result = await owner.PostAsJsonAsync("/api/tasks", new
        {
            title = "  Write the README  ",
            description = "List the next steps.",
            dueDate,
            ownerId = otherId,
            status = "completed",
        });
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var created = await result.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Write the README", created.Title);
        Assert.Equal("List the next steps.", created.Description);
        Assert.Equal(dueDate, created.DueDate);
        Assert.Equal("open", created.Status);

        var location = result.Headers.Location;
        Assert.NotNull(location);
        Assert.EndsWith($"/api/tasks/{created.Id}", location.OriginalString);
        Assert.Equal(created, await owner.GetFromJsonAsync<TaskResponse>(location));
        Assert.Contains(created, (await owner.GetFromJsonAsync<TaskResponse[]>("/api/tasks"))!);
        Assert.Empty((await other.GetFromJsonAsync<TaskResponse[]>("/api/tasks"))!);
        using var inaccessible = await other.GetAsync(location);
        Assert.Equal(HttpStatusCode.NotFound, inaccessible.StatusCode);
        using var inaccessibleWrite = await other.PutAsJsonAsync(location, new { title = "Overwrite", status = "completed" });
        Assert.Equal(HttpStatusCode.NotFound, inaccessibleWrite.StatusCode);
        Assert.Equal(created, await owner.GetFromJsonAsync<TaskResponse>(location));
        using var missing = await owner.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        await using var connection = new NpgsqlConnection(fixture.TasksConnectionString);
        await connection.OpenAsync();
        await using var query = new NpgsqlCommand("SELECT owner_id, due_date FROM tasks.tasks WHERE id = @id", connection);
        query.Parameters.AddWithValue("id", created.Id);
        await using var row = await query.ExecuteReaderAsync();
        Assert.True(await row.ReadAsync());
        Assert.Equal(Guid.Parse("ea9dd2dc-5959-4ca7-b98d-a38923c31aff"), row.GetGuid(0));
        Assert.Equal(dueDate, row.GetFieldValue<DateOnly>(1));
    }

    [Fact]
    public async Task OwnerCanReplaceEditableFieldsAndReopenTheSameTask()
    {
        using var client = fixture.Tasks.CreateClient();
        await AuthenticateAsync(client);
        using var creation = await client.PostAsJsonAsync("/api/tasks", new { title = "Original title" });
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var original = await creation.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(original);

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);
        using var update = await client.PutAsJsonAsync($"/api/tasks/{original.Id}", new
        {
            title = "  Updated title  ",
            description = "Updated description",
            status = "completed",
            dueDate
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var completed = new TaskResponse(original.Id, "Updated title", "Updated description", "completed", dueDate);
        Assert.Equal(completed, await update.Content.ReadFromJsonAsync<TaskResponse>());
        Assert.Contains(completed, (await client.GetFromJsonAsync<TaskResponse[]>("/api/tasks"))!);

        using var reopened = await client.PutAsJsonAsync($"/api/tasks/{original.Id}", new
        {
            title = completed.Title,
            status = "open"
        });
        Assert.Equal(HttpStatusCode.OK, reopened.StatusCode);
        Assert.Equal(new TaskResponse(original.Id, completed.Title, null, "open", null),
            await client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{original.Id}"));
    }

    [Fact]
    public async Task AnOverdueDateCanBeKeptOrClearedButCannotBeChangedToAnotherPastDate()
    {
        using var client = fixture.Tasks.CreateClient();
        await AuthenticateAsync(client);
        using var creation = await client.PostAsJsonAsync("/api/tasks", new { title = "Overdue task" });
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var original = await creation.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(original);

        var overdue = new DateOnly(2000, 1, 1);
        await using (var connection = new NpgsqlConnection(fixture.TasksConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("UPDATE tasks.tasks SET due_date = @dueDate WHERE id = @id", connection);
            command.Parameters.AddWithValue("dueDate", overdue);
            command.Parameters.AddWithValue("id", original.Id);
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }

        var url = $"/api/tasks/{original.Id}";
        using var rejected = await client.PutAsJsonAsync(url,
            new { title = "Rejected edit", status = "completed", dueDate = overdue.AddDays(1) });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        var validation = await rejected.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(validation);
        Assert.Contains("dueDate", validation.Errors.Keys);
        Assert.Equal(original with { DueDate = overdue }, await client.GetFromJsonAsync<TaskResponse>(url));

        using var complete = await client.PutAsJsonAsync(url,
            new { title = original.Title, status = "completed", dueDate = overdue });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.Equal(original with { DueDate = overdue, Status = "completed" },
            await client.GetFromJsonAsync<TaskResponse>(url));

        using var clear = await client.PutAsJsonAsync(url,
            new { title = original.Title, status = "open", dueDate = (DateOnly?)null });
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        Assert.Equal(original, await client.GetFromJsonAsync<TaskResponse>(url));
    }

    [Theory]
    [InlineData("""{"title":"   ","status":"open"}""", "title")]
    [InlineData("""{"title":"Edited","status":"pending"}""", "status")]
    [InlineData("""{"title":"Edited"}""", "status")]
    public async Task InvalidUpdatesLeaveTheStoredTaskUnchanged(string json, string field)
    {
        using var client = fixture.Tasks.CreateClient();
        await AuthenticateAsync(client);
        using var creation = await client.PostAsJsonAsync("/api/tasks", new { title = "Saved task", description = "Original" });
        var original = await creation.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(original);
        var url = $"/api/tasks/{original.Id}";
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var rejected = await client.PutAsync(url, content);

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        var validation = await rejected.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(validation);
        Assert.Contains(field, validation.Errors.Keys);
        Assert.Equal(original, await client.GetFromJsonAsync<TaskResponse>(url));
    }

    [Fact]
    public async Task RequiresAuthenticationForAllTaskRoutes()
    {
        using var client = fixture.Tasks.CreateClient();
        using var create = await client.PostAsJsonAsync("/api/tasks", new { title = "Private" });
        using var list = await client.GetAsync("/api/tasks");
        using var get = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        using var update = await client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}",
            new { title = "Private", status = "open" });

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
    }

    [Theory]
    [InlineData("{}", "title")]
    [InlineData("""{"title":"   "}""", "title")]
    [InlineData("""{"title":"   ","dueDate":"2000-01-01"}""", "title", "dueDate")]
    [InlineData("""{"title":"Task","dueDate":"2000-01-01"}""", "dueDate")]
    [InlineData("""{"title":"Task","dueDate":"tomorrow"}""", "dueDate")]
    [InlineData("""{"title":"Task","timeZone":"Invalid/Zone"}""", "timeZone")]
    [InlineData("""{"title":"Task\u0000"}""", "title")]
    [InlineData("""{"title":"Task","description":"Before\u0000after"}""", "description")]
    public async Task InvalidInputReturnsFieldErrorsAndDoesNotSave(string json, params string[] fields)
    {
        using var client = fixture.Tasks.CreateClient();
        await AuthenticateAsync(client);
        var before = (await client.GetFromJsonAsync<TaskResponse[]>("/api/tasks"))!;

        using var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/tasks", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(error);
        Assert.Equal(fields.Order(), error.Errors.Keys.Order());

        Assert.Equal(before, await client.GetFromJsonAsync<TaskResponse[]>("/api/tasks"));
    }

    [Fact]
    public async Task TodayUsesTheSuppliedTimeZoneAndDueDatesCanBeOmitted()
    {
        await using var host = fixture.Tasks.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(
                new FixedTaskClock(new DateTimeOffset(2026, 11, 23, 1, 0, 0, TimeSpan.Zero)))));
        using var client = host.CreateClient();
        await AuthenticateAsync(client);

        var date = new DateOnly(2026, 11, 22);
        using var local = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Due today locally",
            dueDate = date,
            timeZone = "America/La_Paz"
        });
        Assert.Equal(HttpStatusCode.Created, local.StatusCode);

        using var utc = await client.PostAsJsonAsync("/api/tasks", new { title = "Already past in UTC", dueDate = date });
        Assert.Equal(HttpStatusCode.BadRequest, utc.StatusCode);

        using var noDate = await client.PostAsJsonAsync("/api/tasks", new { title = "No deadline" });
        Assert.Equal(HttpStatusCode.Created, noDate.StatusCode);
        Assert.Null((await noDate.Content.ReadFromJsonAsync<TaskResponse>())?.DueDate);
    }

    private async Task AuthenticateAsync(HttpClient client,
        string email = "demo@example.test", string password = "DemoTasks!2026")
    {
        using var accounts = fixture.Accounts.CreateClient();
        using var response = await accounts.PostAsJsonAsync("/api/accounts/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }
}
