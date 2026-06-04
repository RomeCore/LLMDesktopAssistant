namespace LLMDesktopAssistant.Controls.Toasts;

public class ToastItemViewModel : NotifyPropertyChanged
{
	public required ToastType Type { get; init; }

	public required string Title { get; init; }

	public bool UseMarkdown { get; init; }

	private double _currentOpacity = 1;
	public double CurrentOpacity
	{
		get => _currentOpacity;
		set => SetProperty(ref _currentOpacity, value);
	}

	private double _currentScale = 1;
	public double CurrentScale
	{
		get => _currentScale;
		set => SetProperty(ref _currentScale, value);
	}
}
