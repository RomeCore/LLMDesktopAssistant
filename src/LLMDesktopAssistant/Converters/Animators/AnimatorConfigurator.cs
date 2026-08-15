using Avalonia.Animation;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Converters.Animators
{
	[Service]
	public class AnimatorConfigurator
	{
		public AnimatorConfigurator()
		{
			Animation.RegisterCustomAnimator<string, StringAnimator>();
		}
	}
}