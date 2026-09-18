using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// A card change element that edits a single (usually enum or string) override of an addon with
	/// a selector. While no override exists, the selector shows the reference value taken from the
	/// addon definition or the default.
	/// </summary>
	/// <typeparam name="TAddon">The type of the addon the element is bound to.</typeparam>
	/// <typeparam name="TChange">The type of the change object created by the element.</typeparam>
	/// <typeparam name="TValue">The type of the edited field itself (the nullable field type, e.g. <c>SkillInjectionMode?</c> or <c>string</c>).</typeparam>
	public class AddonCardSelectorChange<TAddon, TChange, TValue> : AddonCardChange, IAddonCardSelectorChange<TValue>
		where TAddon : AddonChangedBase<TAddon, TChange>
		where TChange : AddonChangeBase, new()
	{
		private static readonly EqualityComparer<TValue> _comparer = EqualityComparer<TValue>.Default;

		private readonly AddonCardContext<TAddon, TChange> _context;
		private readonly Func<TAddon, TValue> _getReference;
		private readonly Func<TChange, TValue> _getOverride;
		private readonly Action<TChange, TValue> _setOverride;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardSelectorChange{TAddon, TChange, TValue}"/> class.
		/// </summary>
		/// <param name="context">The context that stores the addon, its set configuration and its change object.</param>
		/// <param name="options">The available selectable options.</param>
		/// <param name="getReference">Reads the reference value of the edited field from the addon definition or the set default.</param>
		/// <param name="getOverride">Reads the override value from the change object. Returns <see langword="default"/> while no override exists.</param>
		/// <param name="setOverride">Writes the override value into the change object.</param>
		public AddonCardSelectorChange(AddonCardContext<TAddon, TChange> context,
			IReadOnlyList<AddonCardSelectorOption<TValue>> options,
			Func<TAddon, TValue> getReference,
			Func<TChange, TValue> getOverride,
			Action<TChange, TValue> setOverride)
		{
			_context = context;
			_getReference = getReference;
			_getOverride = getOverride;
			_setOverride = setOverride;

			Options = [.. options];

			var combo = new ComboBox
			{
				DataContext = this,
				ItemsSource = Options,
				MinWidth = 110,
				FontSize = 12,
				VerticalAlignment = VerticalAlignment.Center,
				ItemTemplate = new FuncDataTemplate<AddonCardSelectorOption<TValue>>((item, _) => new TextBlock
				{
					[!TextBlock.TextProperty] = new Binding(nameof(LocaleKeyBase.Value))
					{
						Source = item.DisplayName
					},
					VerticalAlignment = VerticalAlignment.Center,
				}),
			};
			combo.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(SelectedOption)) { Mode = BindingMode.TwoWay });
			combo.Bind(InputElement.IsEnabledProperty, new Binding(nameof(CanEdit)));
			Content = combo;

			if (CanEdit)
			{
				_context.PropertyChanged += Context_PropertyChanged;
				_context.SetConfig!.PropertyChanged += SetConfig_PropertyChanged;
			}

			SyncIsChanged();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing && CanEdit)
			{
				_context.PropertyChanged -= Context_PropertyChanged;
				_context.SetConfig!.PropertyChanged -= SetConfig_PropertyChanged;
			}
		}

		private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AddonCardContext<,>.Change))
			{
				RaisePropertyChanged(nameof(SelectedOption));
				RaisePropertyChanged(nameof(EffectiveValue));
				SyncIsChanged();
			}
		}

		private void SetConfig_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			RaisePropertyChanged(nameof(SelectedOption));
			RaisePropertyChanged(nameof(EffectiveValue));
		}

		/// <summary>
		/// Gets a value indicating whether the element is allowed to edit the change. Unlike the
		/// enable/hide switches, the selector is not blocked by <see cref="AddonChangedBase{TAddon, TChange}.IsFixed"/>:
		/// the flag only pins the enabled and hidden states of the addon.
		/// </summary>
		public bool CanEdit => _context.SetConfig is not null;

		/// <summary>
		/// Gets the available selectable options.
		/// </summary>
		public ImmutableList<AddonCardSelectorOption<TValue>> Options { get; }

		/// <summary>
		/// Gets the effective value of the edited field: the override value when it exists,
		/// otherwise the reference value read from the addon definition or the set default.
		/// </summary>
		public TValue EffectiveValue
		{
			get
			{
				if (_context.Change is TChange change)
				{
					var value = _getOverride(change);
					if (!IsNone(value))
						return value;
				}

				return _getReference(_context.Addon);
			}
		}

		/// <summary>
		/// Gets or sets the selected option.
		/// </summary>
		public AddonCardSelectorOption<TValue>? SelectedOption
		{
			get => Options.FirstOrDefault(o => _comparer.Equals(o.Value, EffectiveValue));
			set
			{
				if (!CanEdit || value is null || _comparer.Equals(value.Value, EffectiveValue))
					return;

				_setOverride(_context.EnsureChange(), value.Value);
				RaisePropertyChanged(nameof(SelectedOption));
				RaisePropertyChanged(nameof(EffectiveValue));
				SyncIsChanged();
			}
		}

		/// <inheritdoc/>
		public void SetEffectiveValue(TValue value)
		{
			if (!CanEdit || _comparer.Equals(value, EffectiveValue))
				return;

			_setOverride(_context.EnsureChange(), value);
			RaisePropertyChanged(nameof(SelectedOption));
			RaisePropertyChanged(nameof(EffectiveValue));
			SyncIsChanged();
		}

		protected void SyncIsChanged()
		{
			IsChanged = _context.Change is TChange change && !IsNone(_getOverride(change));
		}

		/// <inheritdoc/>
		protected override void ResetCore()
		{
			base.ResetCore();

			if (_context.Change != null)
			{
				_setOverride(_context.Change, default!);
				RaisePropertyChanged(nameof(SelectedOption));
				RaisePropertyChanged(nameof(EffectiveValue));
			}

			SyncIsChanged();
		}

		private static bool IsNone(TValue value) => _comparer.Equals(value, default);
	}
}
