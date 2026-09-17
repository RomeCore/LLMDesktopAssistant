using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using LLMDesktopAssistant.Controls;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// A card change element that edits the LLM model override of an addon with a
	/// <see cref="ModelSelectorControl"/>. While no override exists, the selector shows the model
	/// defined by the addon itself; selecting the empty ("none") model removes the override.
	/// </summary>
	/// <typeparam name="TAddon">The type of the addon the element is bound to.</typeparam>
	/// <typeparam name="TChange">The type of the change object created by the element.</typeparam>
	public class AddonCardModelChange<TAddon, TChange> : AddonCardChange
		where TAddon : AddonChangedBase<TAddon, TChange>
		where TChange : AddonChangeBase, new()
	{
		private readonly AddonCardContext<TAddon, TChange> _context;
		private readonly Func<TAddon, string?> _getDefinitionModel;
		private readonly Func<TChange, string?> _getModelOverride;
		private readonly Action<TChange, string?> _setModelOverride;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardModelChange{TAddon, TChange}"/> class.
		/// </summary>
		/// <param name="context">The context that stores the addon, its set configuration and its change object.</param>
		/// <param name="getDefinitionModel">Reads the model defined by the addon itself, or <see langword="null"/> when the addon has none.</param>
		/// <param name="getModelOverride">Reads the model override from the change object. Returns <see langword="null"/> while no override exists.</param>
		/// <param name="setModelOverride">Writes the model override into the change object. <see langword="null"/> means "no override".</param>
		public AddonCardModelChange(AddonCardContext<TAddon, TChange> context,
			Func<TAddon, string?> getDefinitionModel,
			Func<TChange, string?> getModelOverride,
			Action<TChange, string?> setModelOverride)
		{
			_context = context;
			_getDefinitionModel = getDefinitionModel;
			_getModelOverride = getModelOverride;
			_setModelOverride = setModelOverride;

			var selector = new ModelSelectorControl
			{
				DataContext = this,
				VerticalAlignment = VerticalAlignment.Center,
			};
			selector.Bind(ModelSelectorControl.SelectedModelProperty, new Binding(nameof(SelectedModel)) { Mode = BindingMode.TwoWay });
			selector.Bind(InputElement.IsEnabledProperty, new Binding(nameof(CanEdit)));
			Content = selector;

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
				RaisePropertyChanged(nameof(SelectedModel));
				SyncIsChanged();
			}
		}

		private void SetConfig_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			RaisePropertyChanged(nameof(SelectedModel));
		}

		/// <summary>
		/// Gets a value indicating whether the element is allowed to edit the change. The model
		/// override is not blocked by <see cref="AddonChangedBase{TAddon, TChange}.IsFixed"/>:
		/// the flag only pins the enabled and hidden states of the addon.
		/// </summary>
		public bool CanEdit => _context.SetConfig is not null;

		/// <summary>
		/// Gets or sets the selected model full name, or an empty string for the inherited
		/// (definition) model. Selecting the empty model removes the override instead of storing it.
		/// </summary>
		public string SelectedModel
		{
			get
			{
				if (_context.Change is TChange change && _getModelOverride(change) is { Length: > 0 } model)
					return model;

				return _getDefinitionModel(_context.Addon) ?? string.Empty;
			}
			set
			{
				if (!CanEdit || SelectedModel == value)
					return;

				if (string.IsNullOrEmpty(value))
				{
					if (_context.Change is TChange change)
						_setModelOverride(change, null);
				}
				else
				{
					_setModelOverride(_context.EnsureChange(), value);
				}

				RaisePropertyChanged(nameof(SelectedModel));
				SyncIsChanged();
			}
		}

		protected void SyncIsChanged()
		{
			IsChanged = _context.Change is TChange change && !string.IsNullOrEmpty(_getModelOverride(change));
		}

		/// <inheritdoc/>
		protected override void ResetCore()
		{
			base.ResetCore();

			if (_context.Change != null)
			{
				_setModelOverride(_context.Change, null);
				RaisePropertyChanged(nameof(SelectedModel));
			}

			SyncIsChanged();
		}
	}
}
