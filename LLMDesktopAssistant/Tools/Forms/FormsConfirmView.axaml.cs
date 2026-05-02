using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace LLMDesktopAssistant.Tools.Forms;

public partial class FormsConfirmView : UserControl
{
    public FormsConfirmView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is FormsConfirmViewModel vm)
        {
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(FormsConfirmViewModel.IsDanger))
                {
                    UpdateDangerStyle(vm.IsDanger);
                }
            };

            UpdateDangerStyle(vm.IsDanger);
        }
    }

    private void UpdateDangerStyle(bool isDanger)
    {
        var button = this.FindControl<Button>("ConfirmButton");
        if (button == null) return;

        if (isDanger)
        {
            button.Background = new SolidColorBrush(Color.Parse("#D32F2F"));
            button.Foreground = new SolidColorBrush(Colors.White);
        }
        else
        {
            button.Background = this.FindResource("CardBackground") as IBrush;
            button.Foreground = this.FindResource("DefaultTextBrush") as IBrush;
        }
    }
}
