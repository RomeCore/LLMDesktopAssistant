using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using Serilog;

namespace LLMDesktopAssistant.Providers
{
	/// <summary>
	/// ViewModel for the "Manage Model Providers" dialog.
	/// </summary>
	[ViewModelFor(typeof(ManageModelProvidersDialogView))]
	public class ManageModelProvidersDialogViewModel : ViewModelBase
	{
		private readonly IModelManager _modelManager;

		/// <summary>
		/// Gets the collection of provider display items.
		/// </summary>
		public RangeObservableCollection<ProviderDisplayItem> Providers { get; } = [];

		private ProviderDisplayItem? _selectedProvider;
		/// <summary>
		/// Gets or sets the selected provider.
		/// </summary>
		public ProviderDisplayItem? SelectedProvider
		{
			get => _selectedProvider;
			set
			{
				if (SetProperty(ref _selectedProvider, value))
				{
					NotifyCanExecuteChanged();
					RaisePropertyChanged(nameof(IsProviderSelected));
				}
			}
		}

		/// <summary>
		/// Gets whether a provider is selected.
		/// </summary>
		public bool IsProviderSelected => SelectedProvider != null;

		public IRelayCommand AddCommand { get; }
		public IRelayCommand DeleteCommand { get; }
		public IRelayCommand EditCommand { get; }
		public IRelayCommand CloseCommand { get; }

		public ManageModelProvidersDialogViewModel()
		{
			_modelManager = ServiceRegistry.Provider.GetRequiredService<IModelManager>();

			AddCommand = new RelayCommand(Add);
			DeleteCommand = new RelayCommand(Delete, () => IsProviderSelected);
			EditCommand = new RelayCommand(Edit, () => IsProviderSelected);
			CloseCommand = new RelayCommand(Close);

			ReloadProviders();
		}

		private void NotifyCanExecuteChanged()
		{
			DeleteCommand.NotifyCanExecuteChanged();
			EditCommand.NotifyCanExecuteChanged();
		}

		private void ReloadProviders()
		{
			var providersConfig = SettingsManager.Get<ModelProvidersConfiguration>();
			Providers.Clear();
			foreach (var provider in providersConfig.ModelProviders)
				Providers.Add(new ProviderDisplayItem(provider, _modelManager));
		}

		private async void Add()
		{
			var providersConfig = SettingsManager.Get<ModelProvidersConfiguration>();
			var providerConfig = new ModelProviderConfiguration();
			providersConfig.ModelProviders.Add(providerConfig);
			var vm = new AddModelProviderDialogViewModel(providerConfig);
			var result = await DialogManager.ShowDialogAsync(vm);
			if (result is true)
				ReloadProviders();
		}

		private async void Edit()
		{
			if (SelectedProvider == null)
				return;

			var vm = new AddModelProviderDialogViewModel(SelectedProvider.Source);
			await DialogManager.ShowDialogAsync(vm);
			ReloadProviders();
		}

		private void Delete()
		{
			if (SelectedProvider == null)
				return;

			var providersConfig = SettingsManager.Get<ModelProvidersConfiguration>();
			providersConfig.ModelProviders.Remove(SelectedProvider.Source);
			ReloadProviders();
			SelectedProvider = null;
		}

		private void Close()
		{
			DialogManager.CloseDialog(null);
		}
	}

	/// <summary>
	/// Wrapper around <see cref="ModelProviderConfiguration"/> for display in the manager.
	/// </summary>
	public class ProviderDisplayItem
	{
		private readonly IModelManager _modelManager;

		public ModelProviderConfiguration Source { get; }

		public string Name => Source.Name;
		public string Type => Source.Type;
		public string? EndpointUri => Source.EndpointUri;
		public string ModelCount => LocalizationManager.LocalizeStaticFormat("settings_providers_models_count", Source.Models.Count + Source.CustomModels.Count);

		private string? _connectionStatus;
		public string? ConnectionStatus
		{
			get => _connectionStatus;
			set => _connectionStatus = value;
		}

		public ProviderDisplayItem(ModelProviderConfiguration source, IModelManager modelManager)
		{
			Source = source;
			_modelManager = modelManager;
		}
	}
}
