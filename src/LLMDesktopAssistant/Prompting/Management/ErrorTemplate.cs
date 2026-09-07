using LLTSharp;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting.Management
{
	public class ErrorTemplate : ITemplate
	{
		public IMetadataCollection Metadata => MetadataCollection.Empty;

		/// <summary>
		/// The path to the source file that failed to parse.
		/// </summary>
		public required string SourcePath { get; init; }

		/// <summary>
		/// The exception that occured while parsing the template.
		/// </summary>
		public required Exception Exception { get; init; }

		public object Render(object? context = null, TemplateFunctionSet? functions = null)
		{
			return "This is an error template.";
		}
	}
}
