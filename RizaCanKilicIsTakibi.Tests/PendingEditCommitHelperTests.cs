using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Behaviors;
using RizaCanKilicIsTakibi.Helpers;
using System.Windows.Controls;
using System.Windows.Data;

namespace RizaCanKilicIsTakibi.Tests;

public class PendingEditCommitHelperTests
{
    [Fact]
    public void FlushElement_Updates_TextBox_Source_And_Executes_Attached_Commit_Command()
    {
        RunSta(() =>
        {
            var source = new TestTextSource { Value = "Eski" };
            var textBox = new TextBox();
            var commitCount = 0;

            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(TestTextSource.Value))
            {
                Source = source,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
            });

            TextBoxEditCommitBehavior.SetCommitCommand(textBox, new RelayCommand(() => commitCount++));
            TextBoxEditCommitBehavior.SetCommandParameter(textBox, source);

            textBox.Text = "Yeni";

            Assert.Equal("Eski", source.Value);

            PendingEditCommitHelper.FlushElement(textBox);

            Assert.Equal("Yeni", source.Value);
            Assert.Equal(1, commitCount);
        });
    }

    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new Xunit.Sdk.XunitException($"STA test failed: {failure}");
        }
    }

    private sealed class TestTextSource : ObservableObject
    {
        private string _value = string.Empty;

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}
