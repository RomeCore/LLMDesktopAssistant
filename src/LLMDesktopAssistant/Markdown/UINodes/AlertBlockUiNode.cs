using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using LiveMarkdown.Avalonia;
using Markdig.Extensions.Alerts;
using Material.Icons;
using Material.Icons.Avalonia;

namespace LLMDesktopAssistant.Markdown.UINodes;

/// <summary>
/// Renders a GitHub-style alert block: a rounded border with a type-specific accent
/// color, an icon, a title and the alert body.
/// </summary>
public sealed class AlertBlockUiNode : ContainerBlockNode<AlertBlock>
{
	private const string AlertClass = "AlertBlock";
	private const string RootClass = "AlertBlockRoot";
	private const string HeaderClass = "AlertBlockHeader";
	private const string TitleClass = "AlertBlockTitle";
	private const string BodyClass = "AlertBlockBody";

	private readonly Border _border;
	private readonly MaterialIcon _icon;
	private readonly TextBlock _titleText;
	private string? _currentTypeClass;

	/// <summary>
	/// Gets the border control rendered for the alert.
	/// </summary>
	public override Control Control => _border;

	/// <summary>
	/// Initializes a new alert block node.
	/// </summary>
	public AlertBlockUiNode()
	{
		_icon = new MaterialIcon
		{
			Width = 18,
			Height = 18,
			VerticalAlignment = VerticalAlignment.Center
		};

		_titleText = new TextBlock
		{
			Classes = { TitleClass },
			VerticalAlignment = VerticalAlignment.Center
		};

		var header = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Classes = { HeaderClass }
		};
		header.Children.Add(_icon);
		header.Children.Add(_titleText);

		container.Classes.Add(BodyClass);

		var layout = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Classes = { RootClass }
		};
		layout.Children.Add(header);
		layout.Children.Add(container);

		_border = new Border
		{
			Classes = { AlertClass },
			Child = layout
		};
	}

	/// <inheritdoc/>
	protected override bool UpdateCore(
		DocumentNode documentNode,
		AlertBlock alertBlock,
		in ObservableStringBuilderChangedEventArgs change,
		CancellationToken cancellationToken)
	{
		if (!base.UpdateCore(documentNode, alertBlock, change, cancellationToken))
			return false;

		ApplyAlertType(alertBlock.Kind.ToString());
		return true;
	}

	private void ApplyAlertType(string kind)
	{
		var typeClass = GetTypeClass(kind);
		if (_currentTypeClass is not null)
			_border.Classes.Remove(_currentTypeClass);
		_currentTypeClass = typeClass;
		_border.Classes.Add(typeClass);

		_icon.Kind = GetIcon(kind);
	}

	private static string GetTypeClass(string kind)
	{
		if (string.IsNullOrWhiteSpace(kind))
			return AlertClass + "Note";

		var pascal = char.ToUpperInvariant(kind[0]) + kind[1..].ToLowerInvariant();
		return AlertClass + pascal;
	}

	private static MaterialIconKind GetIcon(string kind) => kind.ToUpperInvariant() switch
	{
		"TIP" => MaterialIconKind.LightbulbOnOutline,
		"IMPORTANT" => MaterialIconKind.AlertOutline,
		"WARNING" => MaterialIconKind.Warning,
		"CAUTION" => MaterialIconKind.AlertOctagonOutline,
		_ => MaterialIconKind.InformationOutline
	};
}
