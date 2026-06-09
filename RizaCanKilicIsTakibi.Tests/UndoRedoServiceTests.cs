using RizaCanKilicIsTakibi.Commands;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class UndoRedoServiceTests
{
    [Fact]
    public void Execute_Undo_Redo_Works_In_Order()
    {
        var service = new UndoRedoService();
        var value = 0;

        service.Execute(new DelegateUndoableAction(
            "increment",
            () => value++,
            () => value--));

        Assert.Equal(1, value);
        Assert.True(service.CanUndo);

        service.Undo();
        Assert.Equal(0, value);
        Assert.True(service.CanRedo);

        service.Redo();
        Assert.Equal(1, value);
    }

    [Fact]
    public void Execute_Trims_Oldest_History_When_MaxHistory_Exceeded()
    {
        var service = new UndoRedoService(maxHistory: 2);
        var value = 0;

        service.Execute(new DelegateUndoableAction("one", () => value += 1, () => value -= 1));
        service.Execute(new DelegateUndoableAction("two", () => value += 1, () => value -= 1));
        service.Execute(new DelegateUndoableAction("three", () => value += 1, () => value -= 1));

        Assert.Equal(3, value);

        service.Undo();
        service.Undo();
        service.Undo();

        Assert.Equal(1, value);
        Assert.False(service.CanUndo);
    }
}
