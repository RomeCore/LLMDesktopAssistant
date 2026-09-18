using System.ComponentModel;
using LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

namespace LLMDesktopAssistant.Tools.MVVM.Elements
{
	/// <summary>
	/// The view model of the policy mask editor of a tool card: one three-state toggle per behaviour
	/// flag declared by the tool. The toggles are the shared <see cref="ToolBehaviourMaskItem"/> instances,
	/// the view model only mirrors the editability of the editor.
	/// </summary>
	[ViewModelFor(typeof(AddonCardToolPolicyMaskView))]
	public sealed class AddonCardToolPolicyMaskViewModel : NotifyPropertyChanged
	{
		private readonly INotifyPropertyChanged _source;
		private readonly Func<bool> _isPolicyBased;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardToolPolicyMaskViewModel"/> class.
		/// </summary>
		/// <param name="source">The block that owns the editor.</param>
		/// <param name="items">The toggles of the mask, one per behaviour flag of the tool.</param>
		/// <param name="isPolicyBased">Whether the effective approval level of the tool applies the policy mask.</param>
		public AddonCardToolPolicyMaskViewModel(INotifyPropertyChanged source,
			ImmutableList<ToolBehaviourMaskItem> items, Func<bool> isPolicyBased)
		{
			_source = source;
			Items = items;
			_isPolicyBased = isPolicyBased;

			_source.PropertyChanged += Source_PropertyChanged;
		}

		/// <summary>
		/// Gets the toggles of the mask.
		/// </summary>
		public ImmutableList<ToolBehaviourMaskItem> Items { get; }

		/// <summary>
		/// Gets a value indicating whether the policy mask is applied by the current approval level of the tool.
		/// </summary>
		public bool IsPolicyBased => _isPolicyBased();

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				_source.PropertyChanged -= Source_PropertyChanged;
		}

		private void Source_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
			RaisePropertyChanged(nameof(IsPolicyBased));
	}
}
