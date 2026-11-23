using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

public sealed class TaskQueries(ITaskRepository tasks) : ITaskQueries
{
    public Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken) =>
        tasks.ListForOwnerAsync(ownerId, cancellationToken);

    public Task<TaskItem?> GetAsync(Guid taskId, Guid ownerId, CancellationToken cancellationToken) =>
        tasks.FindForOwnerAsync(taskId, ownerId, cancellationToken);
}
