using TaskManager.Domain.Tasks;

namespace TaskManager.UnitTests;

public sealed class TaskTitleTests
{
    private static readonly DateOnly Today = new(2026, 11, 22);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void RejectsBlankTitle(string? title)
    {
        var result = TaskItem.Create(Guid.NewGuid(), title, null, null, Today);

        Assert.Null(result.Item);
        Assert.NotNull(result.TitleError);
    }

    [Fact]
    public void TrimsTitleBeforeEnforcingLength()
    {
        var maxLengthTitle = new string('x', 120);
        var valid = TaskItem.Create(Guid.NewGuid(), $" {maxLengthTitle} ", null, null, Today);
        var invalid = TaskItem.Create(Guid.NewGuid(), maxLengthTitle + "x", null, null, Today);

        Assert.Equal(maxLengthTitle, valid.Item?.Title);
        Assert.Null(invalid.Item);
        Assert.NotNull(invalid.TitleError);
    }
}
