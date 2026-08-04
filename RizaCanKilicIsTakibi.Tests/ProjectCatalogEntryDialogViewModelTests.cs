using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class ProjectCatalogEntryDialogViewModelTests
{
    [Fact]
    public void RequiresParent_Shows_When_Istinat_Selected()
    {
        var parent = CreateNormal("Ana İş");
        var vm = new ProjectCatalogEntryDialogViewModel(existing: null, catalog: [parent]);

        Assert.False(vm.RequiresParent);

        vm.SelectedKindChoice = vm.AvailableKinds.First(item => item.Kind == ProjectCatalogKind.Istinat);

        Assert.True(vm.RequiresParent);
        Assert.Equal("İstinat", vm.DisplayName);
        Assert.Contains("Üst proje seçmek", vm.ParentSelectionHint, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_Rejects_Istinat_Without_Parent()
    {
        var parent = CreateNormal("Ana İş");
        var vm = new ProjectCatalogEntryDialogViewModel(existing: null, catalog: [parent]);
        var closed = false;
        vm.RequestClose += (_, _) => closed = true;

        vm.SelectedKindChoice = vm.AvailableKinds.First(item => item.Kind == ProjectCatalogKind.Istinat);
        vm.SaveCommand.Execute(null);

        Assert.False(closed);
        Assert.Equal("İstinat projeleri için üst proje seçilmelidir.", vm.ValidationMessage);
    }

    [Fact]
    public void Save_Accepts_Istinat_With_Parent()
    {
        var parent = CreateNormal("Ana İş");
        var vm = new ProjectCatalogEntryDialogViewModel(existing: null, catalog: [parent]);
        var accepted = false;
        vm.RequestClose += (_, result) => accepted = result;

        vm.SelectedKindChoice = vm.AvailableKinds.First(item => item.Kind == ProjectCatalogKind.Istinat);
        vm.ParentProjectId = parent.Id;
        vm.SaveCommand.Execute(null);

        Assert.True(accepted);
        var entry = vm.BuildEntry();
        Assert.Equal(ProjectCatalogKind.Istinat, entry.Kind);
        Assert.Equal(parent.Id, entry.ParentProjectId);
        Assert.Equal(parent.AdaParsel, entry.AdaParsel);
        Assert.Equal(parent.YapiSahibi, entry.YapiSahibi);
        Assert.Equal(parent.YapiSahibi, entry.Muteahhit);
    }

    [Fact]
    public void ParentSelection_CopiesBlankIdentity_DoesNotOverwriteFilled()
    {
        var parent = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "Ana İş",
            AdaParsel = "1-1",
            YapiSahibi = "Sahip",
            YibfNo = "10",
            Belediye = "Sinop",
            Muteahhit = "Sahip",
            Kind = ProjectCatalogKind.Normal,
            IsActive = true
        };
        var vm = new ProjectCatalogEntryDialogViewModel(existing: null, catalog: [parent]);
        vm.SelectedKindChoice = vm.AvailableKinds.First(item => item.Kind == ProjectCatalogKind.Istinat);
        vm.YapiSahibi = "Elle Yazılan";
        vm.ParentProjectId = parent.Id;

        Assert.Equal("1-1", vm.AdaParsel);
        Assert.Equal("Elle Yazılan", vm.YapiSahibi);
        Assert.Equal("10", vm.YibfNo);
        Assert.Equal("Sinop", vm.Belediye);
        Assert.Equal("Sahip", vm.Muteahhit);
    }

    [Fact]
    public void ParentSelectionHint_When_No_Normal_Parents()
    {
        var vm = new ProjectCatalogEntryDialogViewModel(existing: null, catalog: []);
        vm.SelectedKindChoice = vm.AvailableKinds.First(item => item.Kind == ProjectCatalogKind.Istinat);

        Assert.False(vm.HasParentProjects);
        Assert.Contains("Normal üst proje yok", vm.ParentSelectionHint, StringComparison.Ordinal);

        vm.SaveCommand.Execute(null);
        Assert.Equal("İstinat için önce Normal bir üst proje eklenmelidir.", vm.ValidationMessage);
    }

    private static ProjectCatalogEntry CreateNormal(string displayName)
        => new()
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,
            AdaParsel = "1-1",
            YapiSahibi = "Sahip",
            Kind = ProjectCatalogKind.Normal,
            IsActive = true
        };
}
