using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// A card change element that edits the <see cref="AddonChangeBase.Enabled"/> override of an addon
	/// with a switch. While no override exists, the switch shows the reference value taken from the
	/// addon definition or the default.
	/// </summary>
	/// <typeparam name="TAddon">The type of the addon the element is bound to.</typeparam>
	/// <typeparam name="TChange">The type of the change object created by the element.</typeparam>
	public class AddonCardEnabledChange<TAddon, TChange> : AddonCardChange
		where TAddon : AddonChangedBase<TAddon, TChange>
		where TChange : AddonChangeBase, new()
	{
		private readonly AddonCardContext<TAddon, TChange> _context;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardEnabledChange{TAddon, TChange}"/> class.
		/// </summary>
		/// <param name="addon">The addon the element is bound to.</param>
		/// <param name="changes">The collection that stores the effective changes, or <see langword="null"/> for a read-only element.</param>
		/// <param name="enabledByDefault">The default enabled state used while neither the definition nor a change specifies it.</param>
		public AddonCardEnabledChange(AddonCardContext<TAddon, TChange> context)
		{
			_context = context;
			
			var toggle = new ToggleSwitch
			{
				DataContext = this,
				VerticalAlignment = VerticalAlignment.Center,
			};
			toggle.Bind(ToggleSwitch.IsCheckedProperty, new Binding(nameof(IsEnabled)) { Mode = BindingMode.TwoWay });
			toggle.Bind(InputElement.IsEnabledProperty, new Binding(nameof(CanEdit)));
			toggle.Bind(ToggleSwitch.OffContentProperty, new Binding(nameof(LocaleKeyBase.Value))
			{
				Source = Locale.GetKey("common.off")
			});
			toggle.Bind(ToggleSwitch.OnContentProperty, new Binding(nameof(LocaleKeyBase.Value))
			{
				Source = Locale.GetKey("common.on")
			});
			Content = toggle;

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
				SyncIsChanged();
			}
		}

		private void SetConfig_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AddonSetConfigurationBase<>.EnabledByDefault))
			{
				RaisePropertyChanged(nameof(IsEnabled));
			}
		}

		public bool CanEdit => !_context.Addon.IsFixed && _context.SetConfig is not null;

		/// <summary>
		/// Gets or sets the effective enabled state. Setting the reference value removes the override
		/// instead of storing it.
		/// </summary>
		public bool? IsEnabled
		{
			get => _context.Addon.IsFixed ? true :
				_context.Change?.Enabled ?? _context.Addon.Enabled ?? _context.SetConfig?.EnabledByDefault;
			set
			{
				if (!CanEdit || IsEnabled == value)
					return;

				var change = _context.EnsureChange();
				change.Enabled = value;
				RaisePropertyChanged(nameof(IsEnabled));
				IsChanged = true;
			}
		}

		protected void SyncIsChanged()
		{
			IsChanged = _context.Change?.Enabled is not null;
		}

		/// <inheritdoc/>
		protected override void ResetCore()
		{
			base.ResetCore();

			if (_context.Change != null)
			{
				_context.Change.Enabled = null;
				RaisePropertyChanged(nameof(IsEnabled));
			}

			SyncIsChanged();
		}
	}
}
