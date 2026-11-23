using TaskManager.Application.Tasks;
using TaskManager.Domain.Tasks;

namespace TaskManager.UnitTests;

internal sealed class FakeTaskRepository : ITaskRepository
{
    public List<TaskItem> AddedTasks { get; } = [];

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        AddedTasks.Add(task);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TaskItem>> ListForOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> UpdateAsync(TaskItem task, bool requireCurrentDueDate, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> DeleteForOwnerAsync(Guid taskId, Guid ownerId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<TaskItem?> FindForOwnerAsync(Guid taskId, Guid ownerId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
