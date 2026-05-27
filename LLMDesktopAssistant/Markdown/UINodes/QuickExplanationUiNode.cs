using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.Markdown.Nodes;

namespace LLMDesktopAssistant.Markdown.UINodes;

/// <summary>
/// UI node for rendering quick explanation tooltips inline.
/// Uses a Run with ToolTip attached for hover definitions.
/// </summary>
public class QuickExplanationUiNode : InlineNode<QuickExplanation>
{
	public override Inline Inline { get; }

	private readonly InlineUIContainer _container;
	private readonly Button _button;

	/// <summary>
	/// Attached property to store the QuickExplanation data on the Button.
	/// </summary>
	public static readonly AttachedProperty<string?> QuickExplanationProperty =
		AvaloniaProperty.RegisterAttached<QuickExplanationUiNode, Button, string?>("QuickExplanation");

	public static string? GetQuickExplanation(AvaloniaObject obj) =>
		obj.GetValue(QuickExplanationProperty);

	public static void SetQuickExplanation(AvaloniaObject obj, string? value) =>
		obj.SetValue(QuickExplanationProperty, value);

	public QuickExplanationUiNode()
	{
		_button = new Button();
		_button.Classes.Add("QuickExplanation");

		_container = new InlineUIContainer
		{
			Child = _button
		};

		Inline = _container;
	}

	protected override bool UpdateCore(
		DocumentNode documentNode,
		QuickExplanation explanation,
		in ObservableStringBuilderChangedEventArgs change,
		CancellationToken cancellationToken)
	{
		_button.Content = explanation.Term;
		SetQuickExplanation(_button, explanation.Definition);
		return true;
	}
}
