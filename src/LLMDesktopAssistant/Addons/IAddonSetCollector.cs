using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.Addons
{
	public interface IAddonSetCollector<T>
	{
		IEnumerable<T> GetAvailableAddons();

		IEnumerable<T> GetAddonsForAgent(ChatAgentDescriptor agent);
	}
}
