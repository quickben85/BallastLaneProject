using TaskManager.Application.Tasks;

namespace TaskManager.UnitTests;

public sealed class CreateTaskTests
{
    [Fact]
    public async Task InvalidTitleDoesNotReachTheRepository()
    {
        var tasks = new FakeTaskRepository();
        var useCase = new CreateTask(tasks, TimeProvider.System);

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(), "  ", null, null, TimeZoneInfo.Utc, CancellationToken.None);

        Assert.NotNull(result.TitleError);
        Assert.Empty(tasks.AddedTasks);
    }
}
