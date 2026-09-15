using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons.MVVM
{
	/// <summary>
	/// The base view model of an addon settings page: takes the addons from an addon set collector, renders
	/// each of them with the card of the addon type and filters the list by the search query.
	/// </summary>
	/// <remarks>
	/// Filtering happens before the cards are created and the cards themselves are cached per addon, so a
	/// change of the search query does not rebuild them: the elements of a card contain real controls, and
	/// creating them on every keystroke would be a waste.
	/// </remarks>
	/// <typeparam name="TAddon">The type of the addon shown by the page.</typeparam>
	/// <typeparam name="TChange">The type of the change (override) object of that addon.</typeparam>
	public abstract class AddonListViewModel<TAddon, TChange> : ViewModelBase
		where TAddon : AddonChangedBase<TAddon, TChange>, new()
		where TChange : AddonChangeBase, new()
	{
		private readonly IAddonSetCollector<TAddon> _collector;
		private readonly IAddonCardFactory<TAddon, TChange> _factory;
		private readonly IAddonManagerInvalidator _invalidator;
		private readonly Dictionary<string, AddonCardViewModel> _cards = new(StringComparer.Ordinal);

		private ImmutableList<TAddon> _addons = [];
		private RangeObservableCollection<AddonCardViewModel> _items = [];
		private string _searchText = string.Empty;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonListViewModel{TAddon, TChange}"/> class.
		/// </summary>
		/// <param name="collector">The collector that provides the addons of the page.</param>
		/// <param name="factory">The factory that builds the card of an addon of the page type.</param>
		/// <param name="invalidator">The invalidator used to reload the addons before building the list.</param>
		protected AddonListViewModel(IAddonSetCollector<TAddon> collector, IAddonCardFactory<TAddon, TChange> factory,
			IAddonManagerInvalidator invalidator)
		{
			_collector = collector;
			_factory = factory;
			_invalidator = invalidator;

			TagClickCommand = new RelayCommand<string>(tag =>
			{
				if (!string.IsNullOrEmpty(tag))
					SearchText = tag;
			});
			RefreshCommand = new RelayCommand(Update);
		}

		/// <summary>
		/// Gets the command that filters the list by the tag that was clicked on a card.
		/// </summary>
		public ICommand TagClickCommand { get; }

		/// <summary>
		/// Gets the command that reloads the addons from disk and rebuilds the cards.
		/// </summary>
		public ICommand RefreshCommand { get; }

		/// <summary>
		/// Gets or sets the search query that filters the list by name, description and tags.
		/// </summary>
		public string SearchText
		{
			get => _searchText;
			set
			{
				if (SetProperty(ref _searchText, value))
					ApplyFilter();
			}
		}

		/// <summary>
		/// Gets the cards of the addons that match the current search query.
		/// </summary>
		public RangeObservableCollection<AddonCardViewModel> Items
		{
			get => _items;
			private set => _items.Reset(value);
		}

		/// <summary>
		/// Reloads the addons from disk and rebuilds the cards.
		/// </summary>
		public void Update()
		{
			_invalidator.Reload();

			DisposeCards();
			_addons = [.. GetAddons()];
			ApplyFilter();
		}

		/// <summary>
		/// Gets the addons shown by the page.
		/// </summary>
		protected virtual IEnumerable<TAddon> GetAddons() => _collector.GetAvailableAddons();

		/// <summary>
		/// Creates the context the card of the given addon is built with.
		/// </summary>
		protected abstract AddonCardContext<TAddon, TChange> CreateContext(TAddon addon);

		/// <summary>
		/// Gets a value indicating whether the addon matches the search query.
		/// </summary>
		protected virtual bool Matches(TAddon addon, string query) =>
			addon.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
			|| addon.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
			|| addon.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase));

		private void ApplyFilter()
		{
			var query = SearchText?.Trim() ?? string.Empty;
			RangeObservableCollection<AddonCardViewModel> items = [];

			foreach (var addon in _addons)
			{
				if (query.Length > 0 && !Matches(addon, query))
					continue;

				if (!_cards.TryGetValue(addon.Name, out var card))
				{
					card = _factory.Create(CreateContext(addon));
					_cards[addon.Name] = card;
				}

				items.Add(card);
			}

			Items = items;
		}

		private void DisposeCards()
		{
			foreach (var card in _cards.Values)
				card.Dispose();

			_cards.Clear();
			Items = [];
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				DisposeCards();
		}
	}
}
