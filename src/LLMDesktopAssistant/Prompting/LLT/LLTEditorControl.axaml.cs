using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace LLMDesktopAssistant.Prompting.LLT;

/// <summary>
/// A code editor control for LLT prompt templates with parser-based syntax highlighting.
/// </summary>
public partial class LLTEditorControl : UserControl
{
	/// <summary>
	/// Identifies the <see cref="Text"/> styled property.
	/// </summary>
	public static readonly StyledProperty<string> TextProperty =
		AvaloniaProperty.Register<LLTEditorControl, string>(nameof(Text), defaultValue: string.Empty, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Identifies the <see cref="IsReadOnly"/> styled property.
	/// </summary>
	public static readonly StyledProperty<bool> IsReadOnlyProperty =
		AvaloniaProperty.Register<LLTEditorControl, bool>(nameof(IsReadOnly), defaultValue: false);

	/// <summary>
	/// Gets or sets the text edited by the control.
	/// </summary>
	public string Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the editor is read-only.
	/// </summary>
	public bool IsReadOnly
	{
		get => GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	private readonly LLTTokenClassifier _classifier = new();
	private readonly LLTColorizingTransformer _colorizer;
	private bool _syncing;

	static LLTEditorControl()
	{
		TextProperty.Changed.AddClassHandler<LLTEditorControl>((o, e) => o.OnTextPropertyChanged((string?)e.NewValue));
		IsReadOnlyProperty.Changed.AddClassHandler<LLTEditorControl>((o, e) => o.Editor.IsReadOnly = (bool)e.NewValue!);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LLTEditorControl"/> class.
	/// </summary>
	public LLTEditorControl()
	{
		InitializeComponent();

		_colorizer = new LLTColorizingTransformer(_classifier);
		Editor.TextArea.TextView.LineTransformers.Add(_colorizer);

		Editor.TextChanged += OnEditorTextChanged;
		Editor.Text = Text;
		Editor.IsReadOnly = IsReadOnly;
		_colorizer.Update(Text);
	}

	private void OnEditorTextChanged(object? sender, EventArgs e)
	{
		_colorizer.Update(Editor.Text);
		Editor.TextArea.TextView.Redraw();

		if (_syncing)
			return;

		_syncing = true;
		SetValue(TextProperty, Editor.Text);
		_syncing = false;
	}

	private void OnTextPropertyChanged(string? newValue)
	{
		newValue ??= string.Empty;

		if (!_syncing && Editor.Text != newValue)
		{
			_syncing = true;
			Editor.Text = newValue;
			_syncing = false;
		}

		_colorizer.Update(newValue);
	}
}
