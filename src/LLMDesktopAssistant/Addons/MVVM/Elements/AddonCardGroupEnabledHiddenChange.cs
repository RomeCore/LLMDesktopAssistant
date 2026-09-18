using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using LLMDesktopAssistant.Localization;
using Material.Icons;
using Material.Icons.Avalonia;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// The aggregate enabled/hidden change of a group card: two three-state toggles that show and edit
	/// the joined state of the visible children of the group.
	/// </summary>
	/// <remarks>
	/// The element pairs itself with the state elements of the children by
	/// <see cref="IAddonCardEnabledHiddenChange"/>. Every aggregate value is computed over the children
	/// that are currently visible, a click writes the value only to the children whose effective value
	/// differs, and fixed (or read-only) children are excluded from both the value and the write.
	/// </remarks>
	public class AddonCardGroupEnabledHiddenChange : AddonCardChange
	{
		private static readonly IBrush _positiveBrush = Brushes.LightGreen;
		private static readonly IBrush _negativeBrush = Brushes.OrangeRed;
		private static readonly IBrush _mixedBrush = Brushes.Gray;

		private readonly ImmutableList<(AddonCardViewModel Child, IAddonCardEnabledHiddenChange State)> _states;
		private bool _syncing;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardGroupEnabledHiddenChange"/> class.
		/// </summary>
		/// <param name="children">The cards of the children of the group.</param>
		public AddonCardGroupEnabledHiddenChange(ImmutableList<AddonCardViewModel> children)
		{
			_states = [.. children
				.Select(child => (Child: child, State: child.Changes.OfType<IAddonCardEnabledHiddenChange>().FirstOrDefault()))
				.Where(pair => pair.State is not null)
				.Select(pair => (pair.Child, pair.State!))];

			Content = new StackPanel
			{
				DataContext = this,
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				Children =
				{
					CreateToggle(nameof(EnabledState), nameof(EnabledIcon), nameof(EnabledBrush), "card.group.enabled"),
					CreateToggle(nameof(ShownState), nameof(ShownIcon), nameof(ShownBrush), "card.group.hidden"),
				},
			};

			foreach (var (child, state) in _states)
			{
				child.PropertyChanged += Child_PropertyChanged;

				if (state is INotifyPropertyChanged notifier)
					notifier.PropertyChanged += State_PropertyChanged;
			}

			SyncIsChanged();
		}

		/// <summary>
		/// Gets a value indicating whether any visible child can be edited, i.e. whether the toggles
		/// are usable.
		/// </summary>
		public bool CanToggle => _states.Any(pair => pair.Child.IsVisible && pair.State.CanEdit);

		/// <summary>
		/// Gets or sets the aggregate enabled state: <see langword="true"/> when every editable visible
		/// child is enabled, <see langword="false"/> when none of them is, and <see langword="null"/>
		/// when the states are mixed.
		/// </summary>
		public bool? EnabledState
		{
			get => Aggregate(state => state.IsEnabled);
			set
			{
				if (_syncing || !CanToggle)
					return;

				// The click cycles the group: an enabled group gets disabled, everything else gets enabled.
				Apply(state => state.IsEnabled, (state, target) => state.IsEnabled = target, Aggregate(state => state.IsEnabled) != true);
			}
		}

		/// <summary>
		/// Gets or sets the aggregate shown state (the inverse of the aggregate hidden state):
		/// <see langword="true"/> when every editable visible child is shown, <see langword="false"/>
		/// when all of them are hidden, and <see langword="null"/> when the states are mixed.
		/// </summary>
		public bool? ShownState
		{
			get => Aggregate(state => state.IsHidden) is bool hidden ? !hidden : null;
			set
			{
				if (_syncing || !CanToggle)
					return;

				// The click cycles the group: an all-shown group gets hidden, everything else gets shown.
				Apply(state => state.IsHidden, (state, target) => state.IsHidden = target, Aggregate(state => state.IsHidden) == false);
			}
		}

		/// <summary>
		/// Gets the icon of the enabled toggle for the current aggregate state.
		/// </summary>
		public MaterialIconKind EnabledIcon => EnabledState switch
		{
			true => MaterialIconKind.Check,
			false => MaterialIconKind.Close,
			_ => MaterialIconKind.MinusCircle
		};

		/// <summary>
		/// Gets the brush of the enabled toggle for the current aggregate state.
		/// </summary>
		public IBrush EnabledBrush => EnabledState switch
		{
			true => _positiveBrush,
			false => _negativeBrush,
			_ => _mixedBrush
		};

		/// <summary>
		/// Gets the icon of the shown toggle for the current aggregate state.
		/// </summary>
		public MaterialIconKind ShownIcon => ShownState switch
		{
			true => MaterialIconKind.Eye,
			false => MaterialIconKind.EyeOff,
			_ => MaterialIconKind.MinusCircle
		};

		/// <summary>
		/// Gets the brush of the shown toggle for the current aggregate state.
		/// </summary>
		public IBrush ShownBrush => ShownState switch
		{
			true => _positiveBrush,
			false => _negativeBrush,
			_ => _mixedBrush
		};

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
		}

		private bool? Aggregate(Func<IAddonCardEnabledHiddenChange, bool> selector)
		{
			bool has = false, all = true, any = false;

			foreach (var (child, state) in _states)
			{
				if (!child.IsVisible || !state.CanEdit)
					continue;

				has = true;
				var value = selector(state);
				all &= value;
				any |= value;
			}

			if (!has)
				return null;

			return all ? true : any ? null : false;
		}

		private void Apply(Func<IAddonCardEnabledHiddenChange, bool> get,
			Action<IAddonCardEnabledHiddenChange, bool> set, bool target)
		{
			_syncing = true;

			try
			{
				foreach (var (child, state) in _states)
				{
					if (!child.IsVisible || !state.CanEdit || get(state) == target)
						continue;

					set(state, target);
				}
			}
			finally
			{
				_syncing = false;
			}

			RaiseStateChanged();
		}

		private void RaiseStateChanged()
		{
			RaisePropertyChanged(nameof(CanToggle));
			RaisePropertyChanged(nameof(EnabledState));
			RaisePropertyChanged(nameof(ShownState));
			RaisePropertyChanged(nameof(EnabledIcon));
			RaisePropertyChanged(nameof(EnabledBrush));
			RaisePropertyChanged(nameof(ShownIcon));
			RaisePropertyChanged(nameof(ShownBrush));
			SyncIsChanged();
		}

		private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AddonCardViewModel.IsVisible))
				RaiseStateChanged();
		}

		private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IAddonCardEnabledHiddenChange.IsEnabled) or nameof(IAddonCardEnabledHiddenChange.IsHidden))
				RaiseStateChanged();
			else if (e.PropertyName is nameof(IAddonCardChange.IsChanged))
				SyncIsChanged();
		}

		private void SyncIsChanged()
		{
			IsChanged = _states.Any(pair => pair.Child.IsVisible && pair.State.IsChanged);
		}

		private ToggleButton CreateToggle(string statePath, string iconPath, string brushPath, string toolTipKey)
		{
			var icon = new MaterialIcon
			{
				Width = 16,
				Height = 16,
				VerticalAlignment = VerticalAlignment.Center
			};
			icon.Bind(MaterialIcon.KindProperty, new Binding(iconPath));
			icon.Bind(MaterialIcon.ForegroundProperty, new Binding(brushPath));

			var toggle = new ToggleButton
			{
				IsThreeState = true,
				VerticalAlignment = VerticalAlignment.Center,
				Padding = new Thickness(2),
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Content = icon,
			};
			toggle.Classes.Add("groupStateToggle");

			toggle.Bind(ToggleButton.IsCheckedProperty, new Binding(statePath) { Mode = BindingMode.TwoWay });
			toggle.Bind(InputElement.IsEnabledProperty, new Binding(nameof(CanToggle)));
			toggle.Bind(ToolTip.TipProperty, new Binding(nameof(LocaleKeyBase.Value))
			{
				Source = Locale.GetKey(toolTipKey)
			});

			return toggle;
		}
	}
}
