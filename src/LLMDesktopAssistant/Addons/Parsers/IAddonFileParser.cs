namespace LLMDesktopAssistant.Addons.Parsers
{
	public interface IAddonFileParser<out T>
	{
		IEnumerable<T> Parse(string content, AddonPathInfo fileInfo);
	}
}
