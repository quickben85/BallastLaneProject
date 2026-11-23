namespace TaskManager.Application.Tasks;

public interface IDeleteTask
{
    Task<bool> ExecuteAsync(Guid id, Guid ownerId, CancellationToken cancellationToken);
}
