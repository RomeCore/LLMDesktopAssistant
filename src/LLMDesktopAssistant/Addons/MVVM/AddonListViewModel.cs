using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.Addons.Search;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons.MVVM
{
	public class ToggleableAddonCardViewModel : NotifyPropertyChanged
	{
		public required object Addon { get; init; }

		public required AddonCardViewModel Card { get; init; }

		public bool IsVisible
		{
			get;
			set => SetProperty(ref field, value);
		}
	}

	/// <summary>
	/// The view model of a reusable addon list: the search query, the cards of the addons that match it
	/// and the commands a host view binds to.
	/// </summary>
	/// <remarks>
	/// The class is intentionally non-generic and abstract: it is used as the <c>x:DataType</c> of the
	/// addon list panel view (generic types cannot be used in compiled bindings), while the list itself
	/// is built by <see cref="AddonListViewModel{TAddon, TChange}"/>. Every page that owns an addon list
	/// view model can embed the panel, no inheritance from the settings view models is required anymore.
	/// </remarks>
	public abstract class AddonListViewModel : ViewModelBase
	{
		private string _searchText = string.Empty;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonListViewModel"/> class.
		/// </summary>
		protected AddonListViewModel()
		{
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
		/// Gets the command that reloads the addons from disk and rebuilds the list.
		/// </summary>
		public ICommand RefreshCommand { get; }

		/// <summary>
		/// Gets or sets the locale key of the search box placeholder.
		/// </summary>
		public LocaleKeyBase SearchPlaceholderKey { get; set; } = Locale.GetConstKey(string.Empty);

		/// <summary>
		/// Gets or sets the locale key of the text shown when no addon matches the search query.
		/// </summary>
		public LocaleKeyBase EmptyTextKey { get; set; } = Locale.GetConstKey(string.Empty);

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
		public RangeObservableCollection<ToggleableAddonCardViewModel> Items
		{
			get => field ??= [];
			protected set => (field ??= []).Reset(value);
		}

		/// <summary>
		/// Reloads the addons from disk and rebuilds the list.
		/// </summary>
		public abstract void Update();

		/// <summary>
		/// Rebuilds <see cref="Items"/> for the current <see cref="SearchText"/>.
		/// </summary>
		protected abstract void ApplyFilter();
	}

	/// <summary>
	/// The addon list implementation: takes the addons from an addon set collector, renders each of them
	/// with the card of the addon type, filters the list by the search query and caches the cards.
	/// </summary>
	/// <remarks>
	/// Filtering happens before the cards are created and the cards themselves are cached per addon, so a
	/// change of the search query does not rebuild them: the elements of a card contain real controls, and
	/// creating them on every keystroke would be a waste. When an <see cref="IAddonSearchService{T}"/> is
	/// provided, the list is filtered (and ordered) by the relevance of the search service; otherwise the
	/// plain substring match is used as a fallback.
	/// </remarks>
	/// <typeparam name="TAddon">The type of the addon shown by the list.</typeparam>
	/// <typeparam name="TChange">The type of the change (override) object of that addon.</typeparam>
	public class AddonListViewModel<TAddon, TChange> : AddonListViewModel
		where TAddon : AddonChangedBase<TAddon, TChange>, new()
		where TChange : AddonChangeBase, new()
	{
		private readonly IAddonSetCollector<TAddon> _collector;
		private readonly IAddonCardFactory<TAddon, TChange> _factory;
		private readonly IAddonManagerInvalidator _invalidator;
		private readonly AddonKind _kind;
		private readonly IAddonSearchService<TAddon>? _searchService;
		private readonly Func<AddonListViewModel<TAddon, TChange>, TAddon, AddonCardContext<TAddon, TChange>>? _contextBuilder;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonListViewModel{TAddon, TChange}"/> class.
		/// </summary>
		/// <param name="collector">The collector that provides the addons of the list.</param>
		/// <param name="factory">The factory that builds the card of an addon.</param>
		/// <param name="invalidator">The invalidator used to reload the addons before building the list.</param>
		/// <param name="kind">The kind of addon to list.</param>
		/// <param name="searchService">The search service used to filter the list by the search query, or
		/// <see langword="null"/> to fall back to the plain substring match.</param>
		/// <param name="contextBuilder">The optional builder of the card context, used when the cards need
		/// a context other than the default one (for example, when they edit the overrides of a set).</param>
		public AddonListViewModel(IAddonSetCollector<TAddon> collector, IAddonCardFactory<TAddon, TChange> factory,
			IAddonManagerInvalidator invalidator, AddonKind kind, IAddonSearchService<TAddon>? searchService = null,
			Func<AddonListViewModel<TAddon, TChange>, TAddon, AddonCardContext<TAddon, TChange>>? contextBuilder = null)
		{
			_collector = collector;
			_factory = factory;
			_invalidator = invalidator;
			_kind = kind;
			_searchService = searchService;
			_contextBuilder = contextBuilder;
		}

		/// <inheritdoc/>
		public override void Update()
		{
			_invalidator.Reload(_kind);

			foreach (var card in Items)
				card.Card.Dispose();
			Items.Reset(GetAddons().Select(a => new ToggleableAddonCardViewModel
			{
				Addon = a,
				Card = _factory.Create(CreateContext(a)),
				IsVisible = true
			}));
			ApplyFilter();
		}

		/// <summary>
		/// Gets the addons shown by the list.
		/// </summary>
		protected virtual IEnumerable<TAddon> GetAddons() => _collector.GetAvailableAddons();

		/// <summary>
		/// Creates the context the card of the given addon is built with.
		/// </summary>
		protected virtual AddonCardContext<TAddon, TChange> CreateContext(TAddon addon)
		{
			if (_contextBuilder is not null)
				return _contextBuilder(this, addon);

			return new AddonCardContext<TAddon, TChange>
			{
				Addon = addon,
				TagClickCommand = TagClickCommand,
				OnDeleted = Update
			};
		}

		/// <summary>
		/// Gets a value indicating whether the addon matches the search query by a plain substring match.
		/// Used only when no search service is provided.
		/// </summary>
		protected virtual bool Matches(TAddon addon, string query) =>
			addon.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
			|| addon.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
			|| addon.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase));

		/// <inheritdoc/>
		protected override void ApplyFilter()
		{
			var query = SearchText?.Trim() ?? string.Empty;

			if (query.Length == 0)
			{
				foreach (var card in Items)
					card.IsVisible = true;
			}
			else if (_searchService is not null)
			{
				var matching = _searchService.Search(query, Items.Select(i => (TAddon)i.Addon), maxResults: 0)
					.Select(result => result.Addon).ToHashSet();
				foreach (var card in Items)
					card.IsVisible = matching.Contains((TAddon)card.Addon);
			}
			else
			{
				foreach (var card in Items)
					card.IsVisible = Matches((TAddon)card.Addon, query);
			}
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				foreach (var card in Items)
					card.Card.Dispose();
				Items.Clear();
			}
		}
	}
}
