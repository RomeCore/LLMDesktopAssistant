using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.Markdown.Nodes;

namespace LLMDesktopAssistant.Markdown.UINodes;

/// <summary>
/// UI node for rendering quick action buttons inline.
/// Uses InlineUIContainer to embed a Button inside the text flow.
/// </summary>
public class QuickActionUiNode : InlineNode<QuickAction>
{
	public override Inline Inline { get; }

	private readonly InlineUIContainer _container;
	private readonly Button _button;

	/// <summary>
	/// Callback to invoke when a quick action button is clicked.
	/// Receives the prompt text to insert.
	/// </summary>
	public static Action<string>? OnActionClicked { get; set; }

	/// <summary>
	/// Attached property to store the QuickAction data on the Button.
	/// </summary>
	public static readonly AttachedProperty<QuickAction?> QuickActionProperty =
		AvaloniaProperty.RegisterAttached<QuickActionUiNode, Button, QuickAction?>("QuickAction");

	public static QuickAction? GetQuickAction(AvaloniaObject obj) =>
		obj.GetValue(QuickActionProperty);

	public static void SetQuickAction(AvaloniaObject obj, QuickAction? value) =>
		obj.SetValue(QuickActionProperty, value);

	public QuickActionUiNode()
	{
		_button = new Button();
		_button.Classes.Add("QuickAction");
		_button.Click += OnButtonClick;

		_container = new InlineUIContainer
		{
			Child = _button
		};

		Inline = _container;
	}

	private void OnButtonClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn)
		{
			var action = GetQuickAction(btn);
			if (action != null && !string.IsNullOrEmpty(action.Prompt))
			{
				OnActionClicked?.Invoke(action.Prompt);
			}
		}
	}

	protected override bool UpdateCore(
		DocumentNode documentNode,
		QuickAction quickAction,
		in ObservableStringBuilderChangedEventArgs change,
		CancellationToken cancellationToken)
	{
		_button.Content = quickAction.ButtonText;
		SetQuickAction(_button, quickAction);
		return true;
	}
}
