using System.ComponentModel;
using LLMDesktopAssistant.LLM.MVVM.Settings.Agents;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tools.MVVM.Elements
{
	/// <summary>
	/// The view model of the specifier editor shown inside a collapsible block of a tool card.
	/// The block owns the rules and the modes; the view model adapts them for the bindings.
	/// </summary>
	[ViewModelFor(typeof(AddonCardToolSpecifiersView))]
	public sealed class AddonCardToolSpecifiersViewModel : NotifyPropertyChanged
	{
		private readonly AddonCardToolSpecifiersBlock _owner;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardToolSpecifiersViewModel"/> class.
		/// </summary>
		/// <param name="owner">The block that owns the specifier editor.</param>
		public AddonCardToolSpecifiersViewModel(AddonCardToolSpecifiersBlock owner)
		{
			_owner = owner;
			_owner.PropertyChanged += Owner_PropertyChanged;
		}

		/// <summary>
		/// Gets the selectable specifier behaviour union modes.
		/// </summary>
		public ImmutableList<SpecifierUnionModeItem> UnionModes { get; } = SpecifierUnionModeItem.All;

		/// <summary>
		/// Gets the selectable specifier aggregation modes.
		/// </summary>
		public ImmutableList<SpecifierAggregationModeItem> AggregationModes { get; } = SpecifierAggregationModeItem.All;

		/// <summary>
		/// Gets or sets the specifier behaviour union mode of the tool.
		/// </summary>
		public SpecifierUnionModeItem? UnionMode
		{
			get => _owner.UnionMode;
			set => _owner.UnionMode = value;
		}

		/// <summary>
		/// Gets or sets the specifier aggregation mode of the tool.
		/// </summary>
		public SpecifierAggregationModeItem? AggregationMode
		{
			get => _owner.AggregationMode;
			set => _owner.AggregationMode = value;
		}

		/// <summary>
		/// Gets a value indicating whether the specifier editor is applied by the current approval level of the tool.
		/// </summary>
		public bool IsSectionEnabled => _owner.IsSectionEnabled;

		/// <summary>
		/// Gets a value indicating whether the rule list is editable: the list is disabled when the
		/// specifiers are turned off by the union mode.
		/// </summary>
		public bool IsRulesEnabled => _owner.IsRulesEnabled;

		/// <summary>
		/// Gets the localized hint with the names of the specifier parameters supported by the tool,
		/// or <see langword="null"/> when the tool has no specifier parameters.
		/// </summary>
		public string? SpecifierParametersHint => _owner.SpecifierParametersHint;

		/// <summary>
		/// Gets a value indicating whether the tool declares specifier parameters.
		/// </summary>
		public bool HasParametersHint => _owner.HasParametersHint;

		/// <summary>
		/// Gets the specifier rules of the tool.
		/// </summary>
		public RangeObservableCollection<ToolSpecifierRuleRowViewModel> Rules => _owner.Rules;

		/// <summary>
		/// Gets the command that adds a new specifier rule.
		/// </summary>
		public ICommand AddCommand => _owner.AddCommand;

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				_owner.PropertyChanged -= Owner_PropertyChanged;
		}

		private void Owner_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			RaisePropertyChanged(nameof(UnionMode));
			RaisePropertyChanged(nameof(AggregationMode));
			RaisePropertyChanged(nameof(IsSectionEnabled));
			RaisePropertyChanged(nameof(IsRulesEnabled));
			RaisePropertyChanged(nameof(SpecifierParametersHint));
			RaisePropertyChanged(nameof(HasParametersHint));
		}
	}
}
