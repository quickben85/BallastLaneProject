using TaskManager.Application.Tasks;
using TaskManager.Domain.Tasks;

namespace TaskManager.IntegrationTests;

internal sealed class FailingTaskQueries : ITaskQueries
{
    public Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Internal task query exception marker.");

    public Task<TaskItem?> GetAsync(Guid id, Guid ownerId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
