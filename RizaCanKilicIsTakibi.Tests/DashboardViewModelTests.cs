using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public class DashboardViewModelTests
{
    [Fact]
    public void Refresh_Computes_All_Metrics_Correctly()
    {
        var vm = new DashboardViewModel();

        var tasks = new List<TaskItem>
        {
            new() { BoardType = TaskBoardType.Genel, Notes = { new TaskNote { Text = "n1" } } },
            new() { BoardType = TaskBoardType.Acil, Notes = { new TaskNote { Text = "n2" }, new TaskNote { Text = "n3" } } },
            new() { BoardType = TaskBoardType.Genel },
            new() { BoardType = TaskBoardType.Acil }
        };

        vm.Refresh(tasks);

        Assert.Equal(4, vm.TotalJobs);
        Assert.Equal(2, vm.UrgentJobs);
        Assert.Equal(2, vm.GeneralJobs);
        Assert.Equal(3, vm.TotalNotes);
    }
}
