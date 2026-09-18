using LLMDesktopAssistant.StructuredValues.Parameterization;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// The view model of the parameters editor shown inside a collapsible block of a card.
	/// The editor is materialized by the owning element on the first expansion of the block:
	/// a card the user never touched must stay clean, so both <see cref="Schema"/> and
	/// <see cref="Value"/> stay <see langword="null"/> until then.
	/// </summary>
	[ViewModelFor(typeof(AddonCardParametersView))]
	public sealed class AddonCardParametersViewModel : NotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets the parameter schema to edit, or <see langword="null"/> while the editor is not materialized.
		/// </summary>
		public ParameterSchema? Schema
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Gets or sets the parameter value to edit, or <see langword="null"/> while the editor is not materialized.
		/// </summary>
		public ReactiveNodeValue? Value
		{
			get;
			set => SetProperty(ref field, value);
		}
	}
}
