using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System.Linq;

namespace LLMDesktopAssistant.Tools.Forms;

public partial class FormsFilePickerView : UserControl
{
    public FormsFilePickerView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is FormsFilePickerViewModel vm)
        {
            vm.FilePickRequested += OnFilePickRequested;
        }
    }

    private async void OnFilePickRequested(object? sender, EventArgs e)
    {
        if (DataContext is not FormsFilePickerViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var options = new FilePickerOpenOptions
        {
            AllowMultiple = vm.AllowMultiple,
            Title = vm.Title
        };

        if (!string.IsNullOrWhiteSpace(vm.Filter))
        {
            var extensions = vm.Filter
                .Split(';')
                .Select(f => f.Trim().TrimStart('*').TrimStart('.'))
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();

            if (extensions.Count > 0)
            {
                options.FileTypeFilter =
                [
                    new FilePickerFileType("Выбранные файлы")
                    {
                        Patterns = vm.Filter.Split(';').Select(f => f.Trim()).ToList()
                    }
                ];
            }
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

        if (files.Count > 0)
        {
            var paths = files.Select(f => f.TryGetLocalPath() ?? f.Path.ToString()).ToArray();
            vm.SetPaths(paths);
        }
    }
}
