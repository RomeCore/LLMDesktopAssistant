using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using LLMDesktopAssistant.Converters;
using LLMDesktopAssistant.Localization;
using Material.Icons;
using Material.Icons.Avalonia;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// A card change element that edits the <see cref="AddonChangeBase.Enabled"/> and
	/// <see cref="AddonChangeBase.Hidden"/> overrides of an addon with two switches:
	/// the first toggles the addon on and off, the second switches it between shown and hidden.
	/// </summary>
	/// <typeparam name="TAddon">The type of the addon the element is bound to.</typeparam>
	/// <typeparam name="TChange">The type of the change object created by the element.</typeparam>
	public class AddonCardEnabledHiddenChange<TAddon, TChange> : AddonCardChange
		where TAddon : AddonChangedBase<TAddon, TChange>
		where TChange : AddonChangeBase, new()
	{
		private readonly AddonCardContext<TAddon, TChange> _context;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardEnabledHiddenChange{TAddon, TChange}"/> class.
		/// </summary>
		/// <param name="context">The context that stores the addon, its set configuration and its change object.</param>
		public AddonCardEnabledHiddenChange(AddonCardContext<TAddon, TChange> context)
		{
			_context = context;

			Content = new StackPanel
			{
				DataContext = this,
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				Children =
				{
					CreateSwitch(nameof(IsEnabled), true),
					CreateSwitch(nameof(IsHidden), false),
				},
			};

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
				RaisePropertyChanged(nameof(IsEnabled));
				RaisePropertyChanged(nameof(IsHidden));
				SyncIsChanged();
			}
		}

		private void SetConfig_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AddonSetConfigurationBase<>.EnabledByDefault))
				RaisePropertyChanged(nameof(IsEnabled));

			if (e.PropertyName is nameof(AddonSetConfigurationBase<>.HiddenByDefault))
				RaisePropertyChanged(nameof(IsHidden));
		}

		/// <summary>
		/// Gets a value indicating whether the element is allowed to edit the change.
		/// Fixed addons are always enabled and never hidden, so both values are read-only for them.
		/// </summary>
		public bool CanEdit => !_context.Addon.IsFixed && _context.SetConfig is not null;

		/// <summary>
		/// Gets or sets the effective enabled state. Fixed addons are always enabled.
		/// </summary>
		public bool IsEnabled
		{
			get => _context.Addon.IsFixed ? true :
				_context.Change?.Enabled ?? _context.Addon.Enabled ?? _context.SetConfig?.EnabledByDefault ?? true;
			set
			{
				if (!CanEdit || IsEnabled == value)
					return;

				var change = _context.EnsureChange();
				change.Enabled = value;
				RaisePropertyChanged(nameof(IsEnabled));
				SyncIsChanged();
			}
		}

		/// <summary>
		/// Gets or sets the effective hidden state. Fixed addons are never hidden.
		/// </summary>
		public bool IsHidden
		{
			get => _context.Addon.IsFixed ? false :
				_context.Change?.Hidden ?? _context.Addon.Hidden ?? _context.SetConfig?.HiddenByDefault ?? false;
			set
			{
				if (!CanEdit || IsHidden == value)
					return;

				var change = _context.EnsureChange();
				change.Hidden = value;
				RaisePropertyChanged(nameof(IsHidden));
				SyncIsChanged();
			}
		}

		protected void SyncIsChanged()
		{
			IsChanged = _context.Change?.Enabled is not null || _context.Change?.Hidden is not null;
		}

		/// <inheritdoc/>
		protected override void ResetCore()
		{
			base.ResetCore();

			if (_context.Change != null)
			{
				_context.Change.Enabled = null;
				_context.Change.Hidden = null;
				RaisePropertyChanged(nameof(IsEnabled));
				RaisePropertyChanged(nameof(IsHidden));
			}

			SyncIsChanged();
		}

		private ToggleSwitch CreateSwitch(string propertyPath, bool isEnabledSwitch)
		{
			var toggle = new ToggleSwitch
			{
				DataContext = this,
				VerticalAlignment = VerticalAlignment.Center,
			};
			toggle.Bind(ToggleSwitch.IsCheckedProperty, new Binding(propertyPath)
			{
				Mode = BindingMode.TwoWay,
				Converter = isEnabledSwitch ? null : InverseBooleanConverter.Instance
			});
			toggle.Bind(InputElement.IsEnabledProperty, new Binding(nameof(CanEdit)));

			if (isEnabledSwitch)
			{
				toggle.Bind(ToggleSwitch.OffContentProperty, new Binding(nameof(LocaleKeyBase.Value))
				{
					Source = Locale.GetKey("common.off")
				});
				toggle.Bind(ToggleSwitch.OnContentProperty, new Binding(nameof(LocaleKeyBase.Value))
				{
					Source = Locale.GetKey("common.on")
				});
			}
			else
			{
				toggle.OffContent = new MaterialIcon
				{
					Kind = MaterialIconKind.EyeOff
				};
				toggle.OnContent = new MaterialIcon
				{
					Kind = MaterialIconKind.Eye
				};
			}
			return toggle;
		}
	}
}
