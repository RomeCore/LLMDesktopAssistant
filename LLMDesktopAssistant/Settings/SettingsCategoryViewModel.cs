using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Core.Localization.Resources;
using LLMDesktopAssistant.Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LLMDesktopAssistant.Core.Settings
{
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
		private readonly string _defaultIdLocalized = Locale.settings_default_id;

		public static SettingsCategory<TSettings> Category { get; } = SettingsManager.GetCategory<TSettings>();

		public IEnumerable<string> Ids => Category.GetAvailableIds().Select(c => c == SettingsObject.DefaultId ? 
			Locale.settings_default_id : c);

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

		public string CurrentId
		{
			get => Current == null || Current.Id == SettingsObject.DefaultId ?
				_defaultIdLocalized : Current.Id;
			set
			{
				if (value == _defaultIdLocalized || string.IsNullOrWhiteSpace(value))
					value = SettingsObject.DefaultId;
				if (Current != null && value == Current.Id)
					return;

				Current = Category.Get(value);
				CurrentViewModel = _vmFactory(Current);
				RaisePropertyChanged(null);
				_changed?.Invoke(Current);
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
			CurrentId = initialId;

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
				NewId = null;
			});
			RemoveIdCommand = new RelayCommand(() =>
			{
				if (Category.Remove(Current.Id))
				{
					_current = null!;
					CurrentId = _defaultIdLocalized;
				}
			});
			ConfirmEditIdCommand = new RelayCommand(() =>
			{
				switch (_mode)
				{
					case IdEditMode.Create:

						if (!string.IsNullOrWhiteSpace(NewId) && Category.Copy(Current.Id, NewId))
						{
							CurrentId = NewId;
							IsEditingId = false;
							NewId = null;
						}

						break;

					case IdEditMode.Rename:

						if (NewId == _defaultIdLocalized)
							NewId = SettingsObject.DefaultId;
						if (!string.IsNullOrWhiteSpace(NewId) &&
							NewId != CurrentId &&
							Category.Rename(Current.Id, NewId))
						{
							Category.Get(SettingsObject.DefaultId); // Ensure default settings are loaded if they were renamed.
							RaisePropertyChanged(nameof(Ids));
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