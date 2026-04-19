using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LiveMarkdown.Avalonia;
using UglyToad.PdfPig.Graphics.Operations.TextObjects;

namespace LLMDesktopAssistant.Controls;

public partial class MarkdownControl : UserControl
{
	public static readonly StyledProperty<string> MarkdownTextProperty =
		AvaloniaProperty.Register<MarkdownControl, string>(
			nameof(MarkdownText));

	public string MarkdownText
	{
		get => GetValue(MarkdownTextProperty);
		set => SetValue(MarkdownTextProperty, value);
	}

	static MarkdownControl()
	{
		MarkdownTextProperty.Changed.AddClassHandler<MarkdownControl>((o, e) => o.MarkdownTextChanged(e.OldValue as string, e.NewValue as string));
	}

	private readonly ObservableStringBuilder _markdownBuilder = new();

	public MarkdownControl()
	{
		InitializeComponent();

		MarkdownRenderer.MarkdownBuilder = _markdownBuilder;
	}

	private void MarkdownTextChanged(string? oldText, string? newText)
	{
		oldText ??= string.Empty;
		newText ??= string.Empty;

		if (!newText.StartsWith(oldText))
			_markdownBuilder.Clear();
		string delta = newText[_markdownBuilder.Length..];
		_markdownBuilder.Append(delta);
	}
}