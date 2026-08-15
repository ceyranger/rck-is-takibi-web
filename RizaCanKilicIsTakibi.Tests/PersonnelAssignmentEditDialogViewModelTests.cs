using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class PersonnelAssignmentEditDialogViewModelTests
{
    [Fact]
    public void BuildUpdatedAssignment_MapsEditedFields()
    {
        var person = new Personnel { Name = "Ali" };
        var assignment = new PersonnelAssignment
        {
            PersonnelId = person.Id,
            SourceModule = PersonnelAssignmentSourceModule.Karot,
            SourceEntryId = Guid.NewGuid(),
            SummarySnapshot = "eski",
            Status = PersonnelAssignmentStatus.Open,
            PrioritySnapshot = PersonnelAssignmentPriority.Warning,
            ModuleLabelSnapshot = "Karot Takibi"
        };

        var vm = new PersonnelAssignmentEditDialogViewModel(assignment, [person]);
        vm.Summary = "güncel özet";
        vm.FieldLabel = "Kat";
        vm.ProjectIdentity = "Ada/Parsel";
        vm.SelectedStatus = vm.StatusOptions.First(s => s.Value == PersonnelAssignmentStatus.Completed);
        vm.SelectedPriority = vm.PriorityOptions.First(p => p.Value == PersonnelAssignmentPriority.Critical);
        vm.SelectedPersonnel = vm.PersonnelOptions.First(p => p.Id is null);

        var updated = vm.BuildUpdatedAssignment();

        Assert.Null(updated.PersonnelId);
        Assert.Equal("güncel özet", updated.SummarySnapshot);
        Assert.Equal("Kat", updated.FieldLabelSnapshot);
        Assert.Equal("Ada/Parsel", updated.ProjectIdentitySnapshot);
        Assert.Equal(PersonnelAssignmentStatus.Completed, updated.Status);
        Assert.Equal(PersonnelAssignmentPriority.Critical, updated.PrioritySnapshot);
        Assert.Equal(assignment.Id, updated.Id);
        Assert.Equal(assignment.SourceEntryId, updated.SourceEntryId);
    }
}
