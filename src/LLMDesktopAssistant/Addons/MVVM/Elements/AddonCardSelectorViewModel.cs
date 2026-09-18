using System.ComponentModel;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// The view model of an addon selector (an approval level selector of a tool, an injection mode
	/// selector of a skill, a model selector of a sub-agent, ...). The value lives in the element;
	/// the view model only adapts it for the bindings and optionally supplies the "mixed" placeholder
	/// of a group selector.
	/// </summary>
	[ViewModelFor(typeof(AddonCardSelectorView))]
	public sealed class AddonCardSelectorViewModel : NotifyPropertyChanged
	{
		private readonly INotifyPropertyChanged _source;
		private readonly Func<object?> _get;
		private readonly Action<object?> _set;
		private readonly Func<bool> _canEdit;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardSelectorViewModel"/> class.
		/// </summary>
		/// <param name="source">The element that owns the selector.</param>
		/// <param name="options">The selectable options.</param>
		/// <param name="placeholder">The localized placeholder shown while nothing is selected, or <see langword="null"/>.</param>
		/// <param name="get">Reads the currently selected option.</param>
		/// <param name="set">Writes the option selected by the user. The owner is free to normalize the value.</param>
		/// <param name="canEdit">Whether the selector is editable.</param>
		public AddonCardSelectorViewModel(INotifyPropertyChanged source,
			IReadOnlyList<IAddonCardSelectorOption> options, LocaleKeyBase? placeholder,
			Func<object?> get, Action<object?> set, Func<bool> canEdit)
		{
			_source = source;
			Options = options;
			Placeholder = placeholder;
			_get = get;
			_set = set;
			_canEdit = canEdit;

			_source.PropertyChanged += Source_PropertyChanged;
		}

		/// <summary>
		/// Gets the selectable options.
		/// </summary>
		public IReadOnlyList<IAddonCardSelectorOption> Options { get; }

		/// <summary>
		/// Gets the localized placeholder of the selector, or <see langword="null"/>.
		/// A group selector with differing child values shows the "mixed" placeholder.
		/// </summary>
		public LocaleKeyBase? Placeholder { get; }

		/// <summary>
		/// Gets or sets the selected option.
		/// </summary>
		public object? SelectedOption
		{
			get => _get();
			set
			{
				_set(value);

				// The owner may normalize the value (a group selector, for example, writes the value
				// only to the children whose effective value differs).
				Refresh();
			}
		}

		/// <summary>
		/// Gets a value indicating whether the selector is editable.
		/// </summary>
		public bool CanEdit => _canEdit();

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				_source.PropertyChanged -= Source_PropertyChanged;
		}

		private void Source_PropertyChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

		private void Refresh()
		{
			RaisePropertyChanged(nameof(SelectedOption));
			RaisePropertyChanged(nameof(CanEdit));
		}
	}
}
