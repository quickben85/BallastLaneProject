using Dapper;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace TaskManager.Infrastructure.Accounts;

internal sealed class DapperUserStore(NpgsqlDataSource dataSource) : IUserPasswordStore<IdentityUser<Guid>>
{
    private const string UserColumns = """
        SELECT id, user_name AS UserName, normalized_user_name AS NormalizedUserName,
               password_hash AS PasswordHash, concurrency_stamp AS ConcurrencyStamp
        FROM accounts.users
        """;

    public Task<IdentityUser<Guid>?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        Guid.TryParse(userId, out var id)
            ? FindAsync(UserColumns + " WHERE id = @id", new { id }, cancellationToken)
            : Task.FromResult<IdentityUser<Guid>?>(null);

    public Task<IdentityUser<Guid>?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        FindAsync(UserColumns + " WHERE normalized_user_name = @normalizedUserName",
            new { normalizedUserName }, cancellationToken);

    public async Task<IdentityResult> CreateAsync(IdentityUser<Guid> user, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO accounts.users (id, user_name, normalized_user_name, password_hash, concurrency_stamp)
            VALUES (@Id, @UserName, @NormalizedUserName, @PasswordHash, @ConcurrencyStamp)
            """;

        try
        {
            await ExecuteAsync(sql, user, cancellationToken);
            return IdentityResult.Success;
        }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return IdentityResult.Failed(new IdentityErrorDescriber().DuplicateUserName(user.UserName ?? ""));
        }
    }

    public async Task<IdentityResult> UpdateAsync(IdentityUser<Guid> user, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE accounts.users
            SET user_name = @UserName, normalized_user_name = @NormalizedUserName,
                password_hash = @PasswordHash, concurrency_stamp = @nextStamp
            WHERE id = @Id AND concurrency_stamp = @ConcurrencyStamp
            """;

        var nextStamp = Guid.NewGuid().ToString();
        var arguments = new DynamicParameters(user);
        arguments.Add("nextStamp", nextStamp);
        if (await ExecuteAsync(sql, arguments, cancellationToken) == 0)
        {
            return ConcurrencyFailure();
        }

        user.ConcurrencyStamp = nextStamp;
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(IdentityUser<Guid> user, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM accounts.users WHERE id = @Id AND concurrency_stamp = @ConcurrencyStamp";
        return await ExecuteAsync(sql, user, cancellationToken) == 1
            ? IdentityResult.Success
            : ConcurrencyFailure();
    }

    public Task<string> GetUserIdAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(IdentityUser<Guid> user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(
        IdentityUser<Guid> user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(
        IdentityUser<Guid> user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(IdentityUser<Guid> user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash is not null);

    public void Dispose()
    {
        // DI owns the shared data source.
    }

    private async Task<IdentityUser<Guid>?> FindAsync(
        string sql, object arguments, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<IdentityUser<Guid>>(
            new CommandDefinition(sql, arguments, cancellationToken: cancellationToken));
    }

    private async Task<int> ExecuteAsync(string sql, object arguments, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, arguments, cancellationToken: cancellationToken));
    }

    private static IdentityResult ConcurrencyFailure() =>
        IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());
}
