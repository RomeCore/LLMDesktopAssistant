using System.Text.Json.Serialization;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Represents a serializable text template that can be used in prompts.
	/// </summary>
	public sealed class SerializableTemplate
	{
		static readonly LLTParser _lltParser = new();

		/// <summary>
		/// Represents an empty text template with no content and plain text type.
		/// </summary>
		public static SerializableTemplate Empty { get; } = new("", SerializableTemplateType.PlainText);

		/// <summary>
		/// The source code of the text template. This is used to regenerate the template if necessary.
		/// </summary>
		public string SourceCode { get; }

		/// <summary>
		/// The type of text template. This determines how the template is parsed and rendered.
		/// </summary>
		public SerializableTemplateType Type { get; }

		/// <summary>
		/// The parsed text template. This is used to render the template with specific data.
		/// </summary>
		[JsonIgnore]
		public ITemplate Template { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SerializableTemplate"/> class.
		/// </summary>
		/// <param name="sourceCode">The source code of the text template.</param>
		/// <param name="type">The type of text template.</param>
		/// <exception cref="InvalidDataException">Thrown when the source code does not contain text template.</exception>
		/// <exception cref="ArgumentException">Thrown when the template type is not supported.</exception>
		[JsonConstructor]
		public SerializableTemplate(string sourceCode, SerializableTemplateType type)
		{
			SourceCode = sourceCode;
			Type = type;

			switch (type)
			{
				case SerializableTemplateType.PlainText:
					Template = new PlaintextTemplate(sourceCode);
					break;

				case SerializableTemplateType.LLTText:
					Template = _lltParser.ParseTextTemplate(sourceCode);
					break;

				case SerializableTemplateType.LLTMessages:
					Template = _lltParser.ParseMessagesTemplate(sourceCode);
					break;

				default:
					throw new ArgumentException("Invalid text template type.", nameof(type));
			}
		}

		/// <summary>
		/// Creates a new instance of the <see cref="SerializableTemplate"/> class from an existing text template.
		/// </summary>
		/// <remarks>
		/// This constructor meant to not be used for serialization, just for compability with builtin prompt parts.
		/// </remarks>
		/// <param name="template">The existing text template to create a new instance from.</param>
		public SerializableTemplate(ITemplate template)
		{
			SourceCode = string.Empty;
			Type = SerializableTemplateType.Imported;
			Template = template;
		}
	}
}