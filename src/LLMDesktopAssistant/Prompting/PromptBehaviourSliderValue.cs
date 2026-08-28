namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Represents a single slider value for an agent's behavior slider.
	/// Each slider has a unique identifier and an integer value.
	/// Value 0 represents the default (no modification to prompt).
	/// </summary>
	public class PromptBehaviourSliderValue : PromptPartKeyedSelection<Guid>
	{
		private int _value;
		/// <summary>
		/// The current value of the slider. Range is defined by the slider definition.
		/// 0 means default (no component added to prompt).
		/// </summary>
		public int Value
		{
			get => _value;
			set => SetProperty(ref _value, value);
		}
	}
}
