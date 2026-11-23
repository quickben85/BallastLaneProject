namespace TaskManager.Application.Tasks;

public sealed class DeleteTask(ITaskRepository tasks) : IDeleteTask
{
    public Task<bool> ExecuteAsync(Guid id, Guid ownerId, CancellationToken cancellationToken) =>
        tasks.DeleteForOwnerAsync(id, ownerId, cancellationToken);
}
