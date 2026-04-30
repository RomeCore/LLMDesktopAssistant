using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Settings
{
	public class SettingsIdItemViewModel
	{
		public required string Id { get; init; }
		public string DisplayId => Id == SettingsObject.DefaultId ? LocalizationManager.LocalizeStatic("settings_default_id") : Id;
	
		public static SettingsIdItemViewModel Default { get; } = new SettingsIdItemViewModel { Id = SettingsObject.DefaultId };

		public override bool Equals(object? obj)
		{
			return obj is SettingsIdItemViewModel other && Id == other.Id;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id);
		}
	}

	[ViewModelFor(typeof(SettingsCategoryView))]
	public class SettingsCategoryViewModel<TSettings> : ViewModelBase
		where TSettings : SettingsObject, new()
	{
		private enum IdEditMode
		{
			Create,
			Rename
		}

		private readonly Func<TSettings, ViewModelBase> _vmFactory;
		private readonly Action<TSettings>? _changed;

		public static SettingsCategory<TSettings> Category { get; } = SettingsManager.GetCategory<TSettings>();

		public RangeObservableCollection<SettingsIdItemViewModel> Ids { get; } = [ .. Category.GetAvailableIds()
			.Where(c => c != SettingsObject.DefaultId)
			.Select(c => new SettingsIdItemViewModel { Id = c })
			.Prepend(SettingsIdItemViewModel.Default) ];

		private IdEditMode _mode = IdEditMode.Create;
		private bool _isCreatingNewId = false;
		/// <summary>
		/// Gets or sets a value indicating whether the user is creating a new settings ID.
		/// </summary>
		public bool IsEditingId
		{
			get => _isCreatingNewId;
			set => SetProperty(ref _isCreatingNewId, value);
		}

		private string? _newId;
		public string? NewId
		{
			get => _newId;
			set => SetProperty(ref _newId, value);
		}

		private TSettings _current = null!;
		public TSettings Current
		{
			get => _current;
			private set => SetProperty(ref _current, value);
		}

		private SettingsIdItemViewModel _currentId = null!;
		public SettingsIdItemViewModel CurrentId
		{
			get => _currentId;
			set
			{
				if (string.IsNullOrEmpty(value?.Id))
					value = SettingsIdItemViewModel.Default;

				if (!Equals(_currentId, value))
				{
					_currentId = value;
					_current = Category.Get(value.Id);
					_changed?.Invoke(Current);
					_currentViewModel = _vmFactory(Current);
					RaisePropertyChanged(null);
				}
			}
		}

		private object? _currentViewModel = null;
		public object? CurrentViewModel
		{
			get => _currentViewModel;
			private set => SetProperty(ref _currentViewModel, value);
		}

		public ICommand CreateNewIdCommand { get; }
		public ICommand RenameIdCommand { get; }
		public ICommand RemoveIdCommand { get; }
		public ICommand ConfirmEditIdCommand { get; }
		public ICommand CancelEditIdCommand { get; }

		public SettingsCategoryViewModel(Func<TSettings, ViewModelBase> vmFactory,
			Action<TSettings>? changed = null, string initialId = SettingsObject.DefaultId)
		{
			_vmFactory = vmFactory;
			_changed = changed;
			CurrentId = new SettingsIdItemViewModel { Id = initialId };

			CreateNewIdCommand = new RelayCommand(() =>
			{
				_mode = IdEditMode.Create;
				IsEditingId = true;
				NewId = null;
			});
			RenameIdCommand = new RelayCommand(() =>
			{
				_mode = IdEditMode.Rename;
				IsEditingId = true;
				NewId = Current.Id;
			});
			RemoveIdCommand = new RelayCommand(() =>
			{
				if (Category.Remove(Current.Id))
				{
					if (Current.Id != SettingsObject.DefaultId)
						Ids.Remove(new SettingsIdItemViewModel { Id = Current.Id });
					_current = null!;
					CurrentId = SettingsIdItemViewModel.Default;
				}
			});
			ConfirmEditIdCommand = new RelayCommand(() =>
			{
				var oldId = Current.Id;
				switch (_mode)
				{
					case IdEditMode.Create:

						if (!string.IsNullOrWhiteSpace(NewId) && Category.Copy(Current.Id, NewId))
						{
							if (NewId != SettingsObject.DefaultId && !Ids.Any(c => c.Id == NewId))
								Ids.Add(new SettingsIdItemViewModel { Id = NewId });

							CurrentId = new SettingsIdItemViewModel { Id = NewId };
							IsEditingId = false;
							NewId = null;
						}

						break;

					case IdEditMode.Rename:

						if (NewId == SettingsIdItemViewModel.Default.DisplayId)
							NewId = SettingsObject.DefaultId;
						if (!string.IsNullOrWhiteSpace(NewId) &&
							NewId != CurrentId.Id &&
							Category.Rename(Current.Id, NewId))
						{
							if (NewId != SettingsObject.DefaultId && !Ids.Any(c => c.Id == NewId))
								Ids.Add(new SettingsIdItemViewModel { Id = NewId });
							if (oldId != SettingsObject.DefaultId)
								Ids.Remove(new SettingsIdItemViewModel { Id = oldId });
							CurrentId = new SettingsIdItemViewModel { Id = NewId };

							Category.Get(SettingsObject.DefaultId); // Ensure default settings are loaded if they were renamed.
							RaisePropertyChanged(nameof(CurrentId));
							IsEditingId = false;
							NewId = null;
						}

						break;
				}
			});
			CancelEditIdCommand = new RelayCommand(() =>
			{
				IsEditingId = false;
				NewId = null;
			});
		}
	}
}