using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using LiveMarkdown.Avalonia;
using UglyToad.PdfPig.Graphics.Operations.TextObjects;

namespace LLMDesktopAssistant.Controls;

public partial class MarkdownControl : UserControl
{
	public static readonly StyledProperty<string> MarkdownTextProperty =
		AvaloniaProperty.Register<MarkdownControl, string>(
			nameof(MarkdownText));

	public static readonly StyledProperty<bool> UsePlaintextProperty =
		AvaloniaProperty.Register<MarkdownControl, bool>(
			nameof(UsePlaintext));

	public string MarkdownText
	{
		get => GetValue(MarkdownTextProperty);
		set => SetValue(MarkdownTextProperty, value);
	}

	public bool UsePlaintext
	{
		get => GetValue(UsePlaintextProperty);
		set => SetValue(UsePlaintextProperty, value);
	}

	static MarkdownControl()
	{
		MarkdownTextProperty.Changed.AddClassHandler<MarkdownControl>((o, e) => o.MarkdownTextChanged(e.NewValue as string, o.UsePlaintext));
		UsePlaintextProperty.Changed.AddClassHandler<MarkdownControl>((o, e) => o.MarkdownTextChanged(o.MarkdownText, (bool)e.NewValue!));
	}

	private readonly ObservableStringBuilder _markdownBuilder = new();

	public MarkdownControl()
	{
		InitializeComponent();

		MarkdownRenderer.MarkdownBuilder = _markdownBuilder;
	}

	private void MarkdownTextChanged(string? newText, bool usePlaintext)
	{
		newText ??= string.Empty;

		if (usePlaintext)
		{
			_markdownBuilder.Clear();
			MarkdownTextBlock.Inlines = [new Run(newText)];
		}
		else
		{
			MarkdownTextBlock.Inlines?.Clear();
			var oldText = _markdownBuilder.ToString();
			if (!newText.StartsWith(oldText))
				_markdownBuilder.Clear();
			string delta = newText[_markdownBuilder.Length..];
			if (!string.IsNullOrEmpty(delta))
				_markdownBuilder.Append(delta);
		}
	}
}