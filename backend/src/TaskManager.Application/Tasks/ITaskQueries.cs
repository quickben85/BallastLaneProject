using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

public interface ITaskQueries
{
    Task<IReadOnlyList<TaskItem>> ListAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<TaskItem?> GetAsync(Guid taskId, Guid ownerId, CancellationToken cancellationToken);
}
