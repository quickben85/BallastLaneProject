using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskItem>> ListForOwnerAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<TaskItem?> FindForOwnerAsync(Guid taskId, Guid ownerId, CancellationToken cancellationToken);
}
