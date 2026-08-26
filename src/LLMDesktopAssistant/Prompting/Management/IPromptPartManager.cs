using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	public interface IPromptPartManager<K, V> : IImportablePromptPartManager
		where K : notnull
		where V : PromptPartBase
	{
		V? TryGet(K key);

		ITemplate? TryGetTemplate(K key);

		V? TryGet(ITemplate template);

		IEnumerable<V> GetAll();

		IEnumerable<V> GetAll(PromptPartSource templateSource);
	}
}
