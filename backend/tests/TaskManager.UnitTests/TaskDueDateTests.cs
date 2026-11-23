using TaskManager.Domain.Tasks;
using TaskStatus = TaskManager.Domain.Tasks.TaskStatus;

namespace TaskManager.UnitTests;

public sealed class TaskDueDateTests
{
    private static readonly DateOnly Today = new(2026, 11, 22);

    [Fact]
    public void CanBeOmitted()
    {
        var ownerId = Guid.NewGuid();
        var task = TaskItem.Create(ownerId, "  Plan  ", null, null, Today).Item;

        Assert.NotNull(task);
        Assert.Equal("Plan", task.Title);
        Assert.Equal(ownerId, task.OwnerId);
        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.Null(task.DueDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CanBeTodayOrLater(int days)
    {
        var dueDate = Today.AddDays(days);
        var result = TaskItem.Create(Guid.NewGuid(), "Plan", null, dueDate, Today);

        Assert.Equal(dueDate, result.Item?.DueDate);
    }

    [Fact]
    public void RejectsYesterday()
    {
        var result = TaskItem.Create(Guid.NewGuid(), "Plan", null, Today.AddDays(-1), Today);

        Assert.Null(result.Item);
        Assert.NotNull(result.DueDateError);
    }
}
