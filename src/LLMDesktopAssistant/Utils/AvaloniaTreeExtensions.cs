using Avalonia;

namespace LLMDesktopAssistant.Utils
{
	public static class AvaloniaTreeExtensions
	{
		public static T? FindParent<T>(this StyledElement element)
			where T : StyledElement
		{
			var current = element.Parent;
			while (current != null)
			{
				if (current is T result)
					return result;
				current = current.Parent;
			}
			return null;
		}
	}
}