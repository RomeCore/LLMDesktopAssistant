using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.Management;
using LLMDesktopAssistant.Prompting.Parameterization;
using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	/// <summary>
	/// A lightweight item representing a registered slot element in a picker (combo box).
	/// </summary>
	public class PromptPartOptionViewModel
	{
		public PromptSlotElement Element { get; }

		public PromptPartOptionViewModel(PromptSlotElement element)
		{
			Element = element;
		}

		public Guid Guid => Element.Guid;

		public string Name => Element.Name;

		public string? Category => Element.Category;

		public string? Description => Element.Description;
	}

	/// <summary>
	/// ViewModel for a single "slot" section (system prompt / persona / specialization) of the
	/// agent prompt settings. Combines the "use custom" toggle, the custom text input and the
	/// picker of registered slot elements, plus a parameter editor for the selected element.
	/// The underlying <typeparamref name="TSettings"/> object is exposed so that the view can
	/// bind to <c>Settings.Parameters</c> and <c>SelectedOption.Element.ParameterSchema</c>
	/// (and to the concrete settings properties) directly.
	/// </summary>
	public class PromptSlotSectionViewModel<TSettings> : ViewModelBase
		where TSettings : PromptPartKeyedSelection<Guid>
	{
		private readonly Func<bool> _getUseCustom;
		private readonly Action<bool> _setUseCustom;
		private readonly Func<string?> _getCustomText;
		private readonly Action<string?> _setCustomText;
		private readonly Action _onChanged;
		private readonly PromptSlotKind _kind;
		private bool _isSyncing;
		private PromptPartOptionViewModel? _selectedOption;
		private ReactiveNodeValue? _subscribedValue;

		public PromptSlotSectionViewModel(
			PromptSlotKind kind,
			TSettings settings,
			Func<bool> getUseCustom,
			Action<bool> setUseCustom,
			Func<string?> getCustomText,
			Action<string?> setCustomText,
			IPromptSlotElementManager manager,
			Action onChanged)
		{
			_kind = kind;
			Settings = settings;
			_getUseCustom = getUseCustom;
			_setUseCustom = setUseCustom;
			_getCustomText = getCustomText;
			_setCustomText = setCustomText;
			Manager = manager;
			_onChanged = onChanged;

			ClearCommand = new RelayCommand(Clear);

			RebuildOptions();
			settings.PropertyChanged += Settings_PropertyChanged;
			Sync();
		}

		/// <summary>
		/// The effective settings object for this slot kind.
		/// </summary>
		public TSettings Settings { get; }

		/// <summary>
		/// The manager that provides the registered slot elements.
		/// </summary>
		public IPromptSlotElementManager Manager { get; }

		/// <summary>
		/// Gets the slot kind of this section.
		/// </summary>
		public PromptSlotKind Kind => _kind;

		/// <summary>
		/// Registered slot elements of this kind, shown in the picker.
		/// </summary>
		public ObservableCollection<PromptPartOptionViewModel> Options { get; } = [];

		/// <summary>
		/// Whether the custom text is used instead of a registered element.
		/// </summary>
		public bool UseCustom
		{
			get => _getUseCustom();
			set
			{
				if (_getUseCustom() == value)
					return;
				_setUseCustom(value);
				RaisePropertyChanged();
				RaisePropertyChanged(nameof(IsCustom));
				RaisePropertyChanged(nameof(IsPreset));
			}
		}

		/// <summary>
		/// The custom text, used when <see cref="UseCustom"/> is true.
		/// </summary>
		public string? CustomText
		{
			get => _getCustomText();
			set
			{
				if (_getCustomText() == value)
					return;
				_setCustomText(value);
				RaisePropertyChanged();
			}
		}

		/// <summary>
		/// Gets a value indicating whether the custom text input should be shown.
		/// </summary>
		public bool IsCustom => UseCustom;

		/// <summary>
		/// Gets a value indicating whether the registered element picker should be shown.
		/// </summary>
		public bool IsPreset => !UseCustom;

		/// <summary>
		/// The currently selected registered element, if any.
		/// </summary>
		public PromptPartOptionViewModel? SelectedOption
		{
			get => _selectedOption;
			set
			{
				if (_isSyncing || ReferenceEquals(_selectedOption, value))
					return;
				_selectedOption = value;
				RaisePropertyChanged();
				ApplySelection(value);
			}
		}

		/// <summary>
		/// Gets a value indicating whether the parameter editor should be shown.
		/// </summary>
		public bool HasParameters => SelectedOption?.Element.ParameterSchema is not null && Settings.Parameters is not null;

		/// <summary>
		/// Clears the current selection (deselects the element).
		/// </summary>
		public ICommand ClearCommand { get; }

		/// <summary>
		/// Rebuilds the picker options from the manager.
		/// </summary>
		public void RebuildOptions()
		{
			Options.Clear();
			foreach (var element in Manager.GetAll()
				.Where(e => e.Kind == _kind)
				.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
			{
				Options.Add(new PromptPartOptionViewModel(element));
			}
			Sync();
		}

		/// <summary>
		/// Selects the option with the given GUID. Used by the search picker.
		/// </summary>
		public void SelectOption(Guid guid)
		{
			if (_isSyncing)
				return;
			SelectedOption = Options.FirstOrDefault(o => o.Guid == guid);
		}

		/// <summary>
		/// Deselects the current option (clears the selection).
		/// </summary>
		public void Clear()
		{
			if (_isSyncing)
				return;
			SelectedOption = null;
		}

		private void ApplySelection(PromptPartOptionViewModel? option)
		{
			Settings.Id = option?.Guid ?? Guid.Empty;
			if (option?.Element.ParameterSchema is { } schema)
			{
				var log = new AppendOnlyList<ParameterValidationLogEntry>();
				Settings.Parameters = schema.Root.CreateOrFixValue(Settings.Parameters, log);
			}
			else
			{
				Settings.Parameters = null;
			}
			// Settings_PropertyChanged handles the synchronization and notification.
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			Sync();
			_onChanged();
		}

		private void Sync()
		{
			_isSyncing = true;
			try
			{
				var option = Options.FirstOrDefault(o => o.Guid == Settings.Id);
				if (!ReferenceEquals(_selectedOption, option))
				{
					_selectedOption = option;
					RaisePropertyChanged(nameof(SelectedOption));
				}

				SubscribeValue(Settings.Parameters);
				RaisePropertyChanged(nameof(HasParameters));

				RaisePropertyChanged(nameof(UseCustom));
				RaisePropertyChanged(nameof(IsCustom));
				RaisePropertyChanged(nameof(IsPreset));
				RaisePropertyChanged(nameof(CustomText));
			}
			finally
			{
				_isSyncing = false;
			}
		}

		private void SubscribeValue(ReactiveNodeValue? value)
		{
			if (ReferenceEquals(_subscribedValue, value))
				return;
			if (_subscribedValue is not null)
				_subscribedValue.PropertyChanged -= Value_PropertyChanged;
			_subscribedValue = value;
			if (value is not null)
				value.PropertyChanged += Value_PropertyChanged;
		}

		private void Value_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			_onChanged();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				Settings.PropertyChanged -= Settings_PropertyChanged;
				SubscribeValue(null);
			}
		}
	}
}
