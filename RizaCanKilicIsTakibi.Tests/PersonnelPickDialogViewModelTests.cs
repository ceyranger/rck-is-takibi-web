using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class PersonnelPickDialogViewModelTests
{
    [Fact]
    public void Constructor_WithPersonnel_DoesNotThrowAndSelectsFirst()
    {
        var people = new[]
        {
            new Personnel { Name = "Ali" },
            new Personnel { Name = "Veli" }
        };

        var vm = new PersonnelPickDialogViewModel(people);

        Assert.Equal("Ali", vm.Selected?.Name);
        Assert.True(vm.OkCommand.CanExecute(null));
    }
}
