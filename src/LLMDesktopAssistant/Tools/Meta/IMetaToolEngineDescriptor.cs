using LLMDesktopAssistant.Scripting;

namespace LLMDesktopAssistant.Tools.Meta
{
	public interface IMetaToolEngineDescriptor
	{
		ScriptLanguageType Language { get; }

		string MainExtension { get; }

		string[] Extensions { get; }

		string FrontmatterStart { get; }

		string FrontmatterEnd { get; }

		string Examples { get; }

		string Template { get; }
	}
}