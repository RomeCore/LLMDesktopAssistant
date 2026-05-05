using CommunityToolkit.Mvvm.ComponentModel;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents a single slider value for an agent's behavior slider.
	/// Each slider has a unique identifier and an integer value.
	/// Value 0 represents the default (no modification to prompt).
	/// </summary>
	public class BehaviorSliderValue : NotifyPropertyChanged
	{
		private Guid _sliderId;
		/// <summary>
		/// The unique identifier of the slider definition (matches guid in .llt metadata).
		/// </summary>
		public Guid SliderId
		{
			get => _sliderId;
			set => SetProperty(ref _sliderId, value);
		}

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
