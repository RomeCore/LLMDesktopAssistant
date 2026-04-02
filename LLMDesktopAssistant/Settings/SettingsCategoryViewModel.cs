using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LLMDesktopAssistant.Settings
{
	[ViewModelFor(typeof(SettingsCategoryView))]
	public class SettingsCategoryViewModel<TSettings> : ViewModelBase
		where TSettings : SettingsObject, new()
	{
		private enum IdManipulationMode
		{
			Create,
			Rename
		}


		private readonly Func<TSettings, ViewModelBase> _vmFactory;
		private readonly Action<TSettings>? _changed;

		public static SettingsCategory<TSettings> Category { get; } = SettingsManager.GetCategory<TSettings>();

		public IEnumerable<string> Ids => Category.GetAvailableIds().Select(c => c == SettingsObject.DefaultId ? 
			Locale.settings_default_id : c);

		private bool _isCreatingNewId = false;
		/// <summary>
		/// Gets or sets a value indicating whether the user is creating a new settings ID.
		/// </summary>
		public bool IsCreatingNewId
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
				Locale.settings_default_id : Current.Id;
			set
			{
				if (Current != null && (value == Current.Id ||
					(value == Locale.settings_default_id && Current.Id == SettingsObject.DefaultId)))
					return;
				if (value == Locale.settings_default_id || string.IsNullOrWhiteSpace(value))
					value = SettingsObject.DefaultId;

				Current = Category.Get(value);
				CurrentViewModel = _vmFactory(Current);
				RaisePropertyChanged(nameof(Ids));
				RaisePropertyChanged(nameof(CurrentId));
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
		public ICommand ConfirmNewIdCommand { get; }
		public ICommand CancelNewIdCommand { get; }

		public SettingsCategoryViewModel(Func<TSettings, ViewModelBase> vmFactory,
			Action<TSettings>? changed = null, string initialId = SettingsObject.DefaultId)
		{
			_vmFactory = vmFactory;
			_changed = changed;
			CurrentId = initialId;

			CreateNewIdCommand = new RelayCommand(() =>
			{
				IsCreatingNewId = true;
				NewId = null;
			});
			RenameIdCommand = new RelayCommand(() =>
			{

			});
			ConfirmNewIdCommand = new RelayCommand(() =>
			{
				if (!string.IsNullOrWhiteSpace(NewId))
				{
					CurrentId = NewId;
					IsCreatingNewId = false;
					NewId = null;
				}
			});
			CancelNewIdCommand = new RelayCommand(() =>
			{
				IsCreatingNewId = false;
				NewId = null;
			});
		}
	}
}