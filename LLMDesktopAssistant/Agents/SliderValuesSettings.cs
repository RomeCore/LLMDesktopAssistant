using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the behavior sliders group of an agent prompt: the collection of slider values
	/// for the agent. Each slider has a Guid (matching slider definition in .llt) and integer value.
	/// </summary>
	public class SliderValuesSettings : NotifyPropertyChanged
	{
		private readonly RangeObservableCollection<BehaviorSliderValue> _items = [];
		/// <summary>
		/// Gets or sets the behavior slider values of the agent.
		/// </summary>
		public RangeObservableCollection<BehaviorSliderValue> Items
		{
			get => _items;
			set => _items.Reset(value);
		}
	}
}
