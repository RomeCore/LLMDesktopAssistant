using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// The base/marker interface for prompt context providers. 
	/// </summary>
	public interface IPromptContextProvider
	{
	}

	public interface IPromptSupersedeContextProvider : IPromptContextProvider
	{

	}
}
