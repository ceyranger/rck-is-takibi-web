using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public class ColumnFilterViewModelTests
{
    [Fact]
    public void SetAvailableValues_Drops_Stale_Selection_State()
    {
        var viewModel = new ColumnFilterViewModel("İlçe", () => { }, _ => { });

        viewModel.SetAvailableValues(new[] { "MERKEZ", "GERZE" });
        viewModel.Options.Single(option => option.Value == "GERZE").IsSelected = false;

        viewModel.SetAvailableValues(new[] { "MERKEZ", "AYANCIK" });
        viewModel.SetAvailableValues(new[] { "MERKEZ", "GERZE" });

        Assert.True(viewModel.Options.Single(option => option.Value == "GERZE").IsSelected);
    }

    [Fact]
    public void SetAvailableValues_Preserves_Current_Selection_For_Still_Visible_Value()
    {
        var viewModel = new ColumnFilterViewModel("İlçe", () => { }, _ => { });

        viewModel.SetAvailableValues(new[] { "MERKEZ", "GERZE" });
        viewModel.Options.Single(option => option.Value == "MERKEZ").IsSelected = false;

        viewModel.SetAvailableValues(new[] { "MERKEZ", "AYANCIK" });

        Assert.False(viewModel.Options.Single(option => option.Value == "MERKEZ").IsSelected);
    }
}
