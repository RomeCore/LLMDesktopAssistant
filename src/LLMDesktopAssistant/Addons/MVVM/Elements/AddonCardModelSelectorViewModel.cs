using System.ComponentModel;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// The view model of the model selector shown in the header of a card: it edits the model override
	/// of the addon. The value lives in the element; the view model only adapts it for the bindings.
	/// </summary>
	[ViewModelFor(typeof(AddonCardModelSelectorView))]
	public sealed class AddonCardModelSelectorViewModel : NotifyPropertyChanged
	{
		private readonly INotifyPropertyChanged _source;
		private readonly Func<string> _get;
		private readonly Action<string> _set;
		private readonly Func<bool> _canEdit;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardModelSelectorViewModel"/> class.
		/// </summary>
		/// <param name="source">The element that owns the selector.</param>
		/// <param name="get">Reads the effective model of the addon.</param>
		/// <param name="set">Writes the model selected by the user (an empty string removes the override).</param>
		/// <param name="canEdit">Whether the selector is editable.</param>
		public AddonCardModelSelectorViewModel(INotifyPropertyChanged source,
			Func<string> get, Action<string> set, Func<bool> canEdit)
		{
			_source = source;
			_get = get;
			_set = set;
			_canEdit = canEdit;

			_source.PropertyChanged += Source_PropertyChanged;
		}

		/// <summary>
		/// Gets or sets the selected model full name, or an empty string for the inherited
		/// (definition) model.
		/// </summary>
		public string SelectedModel
		{
			get => _get();
			set
			{
				_set(value);
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
			RaisePropertyChanged(nameof(SelectedModel));
			RaisePropertyChanged(nameof(CanEdit));
		}
	}
}
