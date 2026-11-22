using TaskManager.Application.Accounts;

namespace TaskManager.UnitTests;

internal sealed class StubCredentials(Guid? userId) : IAccountCredentials
{
    public Task<Guid?> VerifyAsync(string email, string password, CancellationToken cancellationToken) =>
        Task.FromResult(userId);
}
