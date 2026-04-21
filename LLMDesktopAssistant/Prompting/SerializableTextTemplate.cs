using LLTSharp;
using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Represents a serializable text template that can be used in prompts.
	/// </summary>
	public sealed class SerializableTextTemplate
	{
		static readonly LLTParser _lltParser = new();

		/// <summary>
		/// Represents an empty text template with no content and plain text type.
		/// </summary>
		public static SerializableTextTemplate Empty { get; } = new("", TextTemplateType.PlainText);

		/// <summary>
		/// The source code of the text template. This is used to regenerate the template if necessary.
		/// </summary>
		public string SourceCode { get; }

		/// <summary>
		/// The type of text template. This determines how the template is parsed and rendered.
		/// </summary>
		public TextTemplateType Type { get; }

		/// <summary>
		/// The parsed text template. This is used to render the template with specific data.
		/// </summary>
		[JsonIgnore]
		public ITextTemplate Template { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SerializableTextTemplate"/> class.
		/// </summary>
		/// <param name="sourceCode">The source code of the text template.</param>
		/// <param name="type">The type of text template.</param>
		/// <exception cref="InvalidDataException">Thrown when the source code does not contain text template.</exception>
		/// <exception cref="ArgumentException">Thrown when the template type is not supported.</exception>
		[JsonConstructor]
		public SerializableTextTemplate(string sourceCode, TextTemplateType type)
		{
			SourceCode = sourceCode;
			Type = type;

			switch (type)
			{
				case TextTemplateType.PlainText:
					Template = new PlaintextTemplate(sourceCode);
					break;

				case TextTemplateType.LLT:
					Template = _lltParser.Parse(sourceCode).OfType<ITextTemplate>().FirstOrDefault() ??
						throw new InvalidDataException("Failed to parse plain text template.");
					break;

				default:
					throw new ArgumentException("Invalid text template type.", nameof(type));
			}
		}

		/// <summary>
		/// Creates a new instance of the <see cref="SerializableTextTemplate"/> class from an existing text template.
		/// </summary>
		/// <remarks>
		/// This constructor meant to not be used for serialization, just for compability with builtin <see cref="PromptComponent"/> and <see cref="Persona"/>.
		/// </remarks>
		/// <param name="template">The existing text template to create a new instance from.</param>
		public SerializableTextTemplate(ITextTemplate template)
		{
			SourceCode = string.Empty;
			Type = TextTemplateType.PlainText;
			Template = template;
		}
	}
}