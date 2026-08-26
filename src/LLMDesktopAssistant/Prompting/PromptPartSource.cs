using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting
{
	public enum PromptPartSource
	{
		/// <summary>
		/// The prompt part came from an unknown source.
		/// </summary>
		Unknown,

		/// <summary>
		/// The prompt part came from template that is embedded inside the application.
		/// </summary>
		BuiltInTemplate,

		/// <summary>
		/// The prompt part came from template that is located inside <see cref="Directories.Templates"/>.
		/// </summary>
		UserTemplate,

		/// <summary>
		/// The prompt part came from template that is located inside the working directory (one selected or all active).
		/// </summary>
		WorkdirTemplate,

		/// <summary>
		/// The prompt part came from configuration.
		/// </summary>
		Configuration
	}
}
