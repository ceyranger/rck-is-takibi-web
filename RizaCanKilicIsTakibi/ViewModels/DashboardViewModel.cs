using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private int _totalJobs;
    private int _urgentJobs;
    private int _generalJobs;
    private int _totalNotes;

    public int TotalJobs
    {
        get => _totalJobs;
        set => SetProperty(ref _totalJobs, value);
    }

    public int UrgentJobs
    {
        get => _urgentJobs;
        set => SetProperty(ref _urgentJobs, value);
    }

    public int GeneralJobs
    {
        get => _generalJobs;
        set => SetProperty(ref _generalJobs, value);
    }

    public int TotalNotes
    {
        get => _totalNotes;
        set => SetProperty(ref _totalNotes, value);
    }

    public void Refresh(IEnumerable<TaskItem> tasks)
    {
        var list = tasks.ToList();
        TotalJobs = list.Count;
        UrgentJobs = list.Count(task => task.BoardType == TaskBoardType.Acil);
        GeneralJobs = list.Count(task => task.BoardType == TaskBoardType.Genel);
        TotalNotes = list.Sum(task => task.Notes.Count);
    }
}
