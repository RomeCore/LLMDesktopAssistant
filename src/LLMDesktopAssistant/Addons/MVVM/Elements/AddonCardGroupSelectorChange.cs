using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// The aggregate change of a group card that edits an override of the visible children with a single
	/// selector: the selector shows the joined value of the children, or the "Mixed" placeholder when the
	/// visible children differ.
	/// </summary>
	/// <remarks>
	/// The element pairs itself with the selector elements of the children by
	/// <see cref="IAddonCardSelectorChange{TValue}"/>. A selected value is written only to the children
	/// whose effective value differs; non-editable children are excluded from both the value and the write.
	/// </remarks>
	/// <typeparam name="TValue">The type of the edited value (the nullable field type, e.g. <c>ToolApprovalLevel?</c>).</typeparam>
	public class AddonCardGroupSelectorChange<TValue> : AddonCardChange
	{
		private static readonly EqualityComparer<TValue> _comparer = EqualityComparer<TValue>.Default;

		private readonly ImmutableList<(AddonCardViewModel Child, IAddonCardSelectorChange<TValue> State)> _states;
		private bool _syncing;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardGroupSelectorChange{TValue}"/> class.
		/// </summary>
		/// <param name="children">The cards of the children of the group.</param>
		/// <param name="options">The selectable options (the same options the children selectors offer, without the mixed state).</param>
		/// <param name="mixedLabel">The localized label shown while the visible children hold different values.</param>
		public AddonCardGroupSelectorChange(ImmutableList<AddonCardViewModel> children,
			ImmutableList<AddonCardSelectorOption<TValue>> options, LocaleKeyBase mixedLabel)
		{
			_states = [.. children
				.Select(child => (Child: child, State: child.Changes.OfType<IAddonCardSelectorChange<TValue>>().FirstOrDefault()))
				.Where(pair => pair.State is not null)
				.Select(pair => (pair.Child, pair.State!))];

			Options = options;

			var combo = new ComboBox
			{
				DataContext = this,
				ItemsSource = options,
				MinWidth = 110,
				FontSize = 12,
				VerticalAlignment = VerticalAlignment.Center,
				ItemTemplate = new FuncDataTemplate<AddonCardSelectorOption<TValue>>((item, _) => new TextBlock
				{
					[!TextBlock.TextProperty] = new Binding(nameof(LocaleKeyBase.Value))
					{
						Source = item?.DisplayName
					},
					VerticalAlignment = VerticalAlignment.Center,
				}),
			};
			combo.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(SelectedOption)) { Mode = BindingMode.TwoWay });
			combo.Bind(InputElement.IsEnabledProperty, new Binding(nameof(CanEdit)));
			combo.Bind(ComboBox.PlaceholderTextProperty, new Binding(nameof(LocaleKeyBase.Value)) { Source = mixedLabel });
			Content = combo;

			foreach (var (child, state) in _states)
			{
				child.PropertyChanged += Child_PropertyChanged;

				if (state is INotifyPropertyChanged notifier)
					notifier.PropertyChanged += State_PropertyChanged;
			}

			SyncIsChanged();
		}

		/// <summary>
		/// Gets a value indicating whether any visible child can be edited, i.e. whether the selector is usable.
		/// </summary>
		public bool CanEdit => _states.Any(pair => pair.Child.IsVisible && pair.State.CanEdit);

		/// <summary>
		/// Gets or sets the selected option. The option is <see langword="null"/> (and the selector shows
		/// the mixed placeholder) while the effective values of the visible children differ.
		/// </summary>
		public AddonCardSelectorOption<TValue>? SelectedOption
		{
			get
			{
				TValue? value = default;
				var has = false;

				foreach (var (child, state) in _states)
				{
					if (!child.IsVisible || !state.CanEdit)
						continue;

					if (!has)
					{
						value = state.EffectiveValue;
						has = true;
						continue;
					}

					if (!_comparer.Equals(value!, state.EffectiveValue))
						return null;
				}

				return has ? Options.FirstOrDefault(option => _comparer.Equals(option.Value, value)) : null;
			}
			set
			{
				if (_syncing || !CanEdit || value is null)
					return;

				Apply(value.Value);
			}
		}

		/// <summary>
		/// Gets the selectable options of the selector.
		/// </summary>
		public ImmutableList<AddonCardSelectorOption<TValue>> Options { get; }

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				foreach (var (child, state) in _states)
				{
					child.PropertyChanged -= Child_PropertyChanged;

					if (state is INotifyPropertyChanged notifier)
						notifier.PropertyChanged -= State_PropertyChanged;
				}
			}
		}

		/// <inheritdoc/>
		protected override void ResetCore()
		{
			base.ResetCore();

			foreach (var (child, state) in _states)
			{
				if (child.IsVisible && state.IsChanged)
					state.ResetCommand?.Execute(null);
			}

			SyncIsChanged();
			RaiseChanged();
		}

		private void Apply(TValue target)
		{
			_syncing = true;

			try
			{
				foreach (var (child, state) in _states)
				{
					if (!child.IsVisible || !state.CanEdit || _comparer.Equals(state.EffectiveValue, target))
						continue;

					state.SetEffectiveValue(target);
				}
			}
			finally
			{
				_syncing = false;
			}

			RaiseChanged();
		}

		private void RaiseChanged()
		{
			RaisePropertyChanged(nameof(CanEdit));
			RaisePropertyChanged(nameof(SelectedOption));
			SyncIsChanged();
		}

		private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AddonCardViewModel.IsVisible))
				RaiseChanged();
		}

		private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IAddonCardSelectorChange<TValue>.EffectiveValue))
				RaiseChanged();
			else if (e.PropertyName is nameof(IAddonCardChange.IsChanged))
				SyncIsChanged();
		}

		private void SyncIsChanged()
		{
			IsChanged = _states.Any(pair => pair.Child.IsVisible && pair.State.IsChanged);
		}
	}
}
