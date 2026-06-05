using Avalonia.Animation;

namespace LLMDesktopAssistant.Converters.Animators
{
	public class SteppingAnimator<T> : InterpolatingAnimator<T>
	{
		public override T Interpolate(double progress, T oldValue, T newValue)
		{
			if (progress < 0.5)
				return oldValue;
			return newValue;
		}
	}
}