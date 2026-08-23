using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace LLMDesktopAssistant.Prompting.LLT;

/// <summary>
/// A <see cref="DocumentColorizingTransformer"/> that applies LLT token classification
/// to the rendered document lines.
/// </summary>
public sealed class LLTColorizingTransformer : DocumentColorizingTransformer
{
	private static readonly IBrush KeywordBrush = CreateBrush("#569CD6");
	private static readonly IBrush IdentifierBrush = CreateBrush("#9CDCFE");
	private static readonly IBrush NumberBrush = CreateBrush("#B5CEA8");
	private static readonly IBrush StringBrush = CreateBrush("#CE9178");
	private static readonly IBrush OperatorBrush = CreateBrush("#D4D4D4");
	private static readonly IBrush ControlBrush = CreateBrush("#C586C0");
	private static readonly IBrush CommentBrush = CreateBrush("#6A9955");
	private static readonly IBrush ErrorBrush = CreateBrush("#F44747");

	private static readonly TextDecorationCollection ErrorDecorations = new()
	{
		new TextDecoration
		{
			Stroke = ErrorBrush,
			StrokeThickness = 1,
			StrokeDashArray = [2, 2]
		}
	};

	private readonly LLTTokenClassifier _classifier;
	private IReadOnlyList<LLTTokenSegment> _segments = [];
	private string _text = string.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="LLTColorizingTransformer"/> class.
	/// </summary>
	/// <param name="classifier">The classifier used to produce token segments.</param>
	public LLTColorizingTransformer(LLTTokenClassifier classifier)
	{
		_classifier = classifier;
	}

	/// <summary>
	/// Reclassifies the document if the specified text differs from the currently classified text.
	/// </summary>
	/// <param name="text">The current document text.</param>
	public void Update(string text)
	{
		text ??= string.Empty;
		if (text == _text)
			return;

		_text = text;
		_segments = _classifier.Classify(text).Segments;
	}

	/// <inheritdoc/>
	protected override void ColorizeLine(DocumentLine line)
	{
		var segments = _segments;
		var lineStart = line.Offset;
		var lineEnd = lineStart + line.Length;

		foreach (var segment in segments)
		{
			if (segment.End <= lineStart || segment.Start >= lineEnd)
				continue;

			var start = Math.Max(segment.Start, lineStart);
			var end = Math.Min(segment.End, lineEnd);

			ChangeLinePart(start, end, element =>
			{
				var properties = element.TextRunProperties;
				properties.SetForegroundBrush(GetBrush(segment.Kind));
				if (segment.Kind == LLTTokenKind.Error)
					properties.SetTextDecorations(ErrorDecorations);
			});
		}
	}

	private static IBrush GetBrush(LLTTokenKind kind)
	{
		return kind switch
		{
			LLTTokenKind.Keyword => KeywordBrush,
			LLTTokenKind.Identifier => IdentifierBrush,
			LLTTokenKind.Number => NumberBrush,
			LLTTokenKind.String => StringBrush,
			LLTTokenKind.Operator => OperatorBrush,
			LLTTokenKind.Control => ControlBrush,
			LLTTokenKind.Comment => CommentBrush,
			LLTTokenKind.Error => ErrorBrush,
			_ => Brushes.Transparent
		};
	}

	private static IBrush CreateBrush(string color)
	{
		return new SolidColorBrush(Color.Parse(color));
	}
}
