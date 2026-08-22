using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Providers
{
	/// <summary>
	/// ViewModel for the "Manage Model Modifiers" dialog.
	/// </summary>
	[ViewModelFor(typeof(ManageModifiersDialogView))]
	public class ManageModifiersDialogViewModel : ViewModelBase
	{
		private readonly ModelModifiersConfiguration _configuration;

		/// <summary>
		/// Gets the collection of modifier display items.
		/// </summary>
		public RangeObservableCollection<ModelModifier> Modifiers => _configuration.Modifiers;

		private ModelModifier? _selectedModifier;
		/// <summary>
		/// Gets or sets the selected modifier.
		/// </summary>
		public ModelModifier? SelectedModifier
		{
			get => _selectedModifier;
			set
			{
				if (SetProperty(ref _selectedModifier, value))
				{
					NotifyCanExecuteChanged();
					RaisePropertyChanged(nameof(IsModifierSelected));
				}
			}
		}

		/// <summary>
		/// Gets whether a modifier is selected.
		/// </summary>
		public bool IsModifierSelected => SelectedModifier != null;

		/// <summary>
		/// Gets the command that adds a new modifier.
		/// </summary>
		public IRelayCommand AddCommand { get; }

		/// <summary>
		/// Gets the command that edits the selected modifier.
		/// </summary>
		public IRelayCommand EditCommand { get; }

		/// <summary>
		/// Gets the command that deletes the selected modifier.
		/// </summary>
		public IRelayCommand DeleteCommand { get; }

		/// <summary>
		/// Gets the command that moves a modifier up in the list.
		/// </summary>
		public IRelayCommand<ModelModifier> MoveUpCommand { get; }

		/// <summary>
		/// Gets the command that moves a modifier down in the list.
		/// </summary>
		public IRelayCommand<ModelModifier> MoveDownCommand { get; }

		/// <summary>
		/// Gets the command that closes the dialog.
		/// </summary>
		public IRelayCommand CloseCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ManageModifiersDialogViewModel"/> class.
		/// </summary>
		public ManageModifiersDialogViewModel()
		{
			_configuration = SettingsManager.Get<ModelModifiersConfiguration>();

			AddCommand = new RelayCommand(Add);
			EditCommand = new RelayCommand(Edit, () => IsModifierSelected);
			DeleteCommand = new RelayCommand(Delete, () => IsModifierSelected);
			MoveUpCommand = new RelayCommand<ModelModifier>(item => Move(item!, -1), item => CanMove(item!, -1));
			MoveDownCommand = new RelayCommand<ModelModifier>(item => Move(item!, 1), item => CanMove(item!, 1));
			CloseCommand = new RelayCommand(() => DialogManager.CloseDialog(null));
		}

		private void NotifyCanExecuteChanged()
		{
			EditCommand.NotifyCanExecuteChanged();
			DeleteCommand.NotifyCanExecuteChanged();
			MoveUpCommand.NotifyCanExecuteChanged();
			MoveDownCommand.NotifyCanExecuteChanged();
		}

		private async void Add()
		{
			var configuration = SettingsManager.Get<ModelModifiersConfiguration>();
			var modifier = new ModelModifier();
			var vm = new ConfigureModifierDialogViewModel(modifier, isEditMode: false);
			var result = await DialogManager.ShowDialogAsync(vm);
			if (result is true)
				configuration.Modifiers.Add(modifier);
		}

		private async void Edit()
		{
			if (SelectedModifier == null)
				return;

			var vm = new ConfigureModifierDialogViewModel(SelectedModifier, isEditMode: true);
			await DialogManager.ShowDialogAsync(vm);
		}

		private void Delete()
		{
			if (SelectedModifier == null)
				return;

			var configuration = SettingsManager.Get<ModelModifiersConfiguration>();
			configuration.Modifiers.Remove(SelectedModifier);
			SelectedModifier = null;
		}

		private bool CanMove(ModelModifier item, int offset)
		{
			var index = Modifiers.IndexOf(item);
			var targetIndex = index + offset;
			return index >= 0 && targetIndex >= 0 && targetIndex < Modifiers.Count;
		}

		private void Move(ModelModifier item, int offset)
		{
			if (!CanMove(item, offset))
				return;

			var configuration = SettingsManager.Get<ModelModifiersConfiguration>();
			var index = Modifiers.IndexOf(item);
			configuration.Modifiers.Move(index, index + offset);
			MoveUpCommand.NotifyCanExecuteChanged();
			MoveDownCommand.NotifyCanExecuteChanged();
		}
	}
}
