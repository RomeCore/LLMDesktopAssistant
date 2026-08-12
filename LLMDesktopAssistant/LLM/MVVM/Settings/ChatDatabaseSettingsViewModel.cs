using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.ApiKeys;
using LLMDesktopAssistant.Data.Connectors;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// A selectable database connector type item for the settings UI.
	/// </summary>
	public class DatabaseConnectorTypeItem
	{
		/// <summary>
		/// Gets all available connector types.
		/// </summary>
		public static readonly ImmutableList<DatabaseConnectorTypeItem> All =
		[
			new() { Value = DatabaseConnectorType.SQLite, DisplayName = "SQLite" },
			new() { Value = DatabaseConnectorType.SQLServer, DisplayName = "SQL Server" },
			new() { Value = DatabaseConnectorType.PostgreSQL, DisplayName = "PostgreSQL" }
		];

		/// <summary>
		/// Gets the connector type value.
		/// </summary>
		public required DatabaseConnectorType Value { get; init; }

		/// <summary>
		/// Gets the display name of the connector type.
		/// </summary>
		public required string DisplayName { get; init; }

		/// <summary>
		/// Returns the item for the specified connector type value.
		/// Falls back to a generated item when the value is not part of <see cref="All"/>
		/// (e.g. a connector type that is not available on the current platform).
		/// </summary>
		/// <param name="value">The connector type value.</param>
		/// <returns>The matching item.</returns>
		public static DatabaseConnectorTypeItem FromValue(DatabaseConnectorType value) =>
			All.FirstOrDefault(i => i.Value == value)
			?? new DatabaseConnectorTypeItem { Value = value, DisplayName = value.ToString() };
	}

	/// <summary>
	/// ViewModel for the Database connections settings tab.
	/// Manages named database connections and the custom connection string.
	/// </summary>
	[ViewModelFor(typeof(ChatDatabaseSettingsView))]
	public class ChatDatabaseSettingsViewModel : ViewModelBase
	{
		private readonly IApiKeyManagerService _apiKeyManager;
		private readonly ImmutableList<IDatabaseConnector> _connectors;

		private bool _isCustomTesting;
		private bool? _customTestResult;
		private string? _customTestResultMessage;

		/// <summary>
		/// Gets the underlying database settings.
		/// </summary>
		public ChatDatabaseSettings DatabaseSettings { get; }

		/// <summary>
		/// Gets the effective database connection configuration resolved by the current inheritance level.
		/// </summary>
		public DatabaseConnectionSettings EffectiveDatabaseConnection => DatabaseSettings.GetEffectiveDatabaseConnection();

		private InheritanceLevelItem _selectedDatabaseConnectionInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the database connections group.
		/// </summary>
		public InheritanceLevelItem SelectedDatabaseConnectionInheritance
		{
			get => _selectedDatabaseConnectionInheritance;
			set
			{
				if (SetProperty(ref _selectedDatabaseConnectionInheritance, value) && value != null)
					DatabaseSettings.DatabaseConnectionInheritance = value.Value;
			}
		}

		/// <summary>
		/// Gets the database connector types available on the current platform
		/// (derived from the registered <see cref="IDatabaseConnector"/> services).
		/// </summary>
		public ImmutableList<DatabaseConnectorTypeItem> ConnectorTypes { get; }

		/// <summary>
		/// Gets or sets the connector type item selected for the custom connection.
		/// </summary>
		public DatabaseConnectorTypeItem SelectedCustomConnectorTypeItem
		{
			get => DatabaseConnectorTypeItem.FromValue(EffectiveDatabaseConnection.CustomConnectorType);
			set
			{
				if (value is not null)
					EffectiveDatabaseConnection.CustomConnectorType = value.Value;
			}
		}

		/// <summary>
		/// Gets the view models for the named database connections.
		/// </summary>
		public RangeObservableCollection<DatabaseConnectionItemViewModel> ConnectionItems { get; } = [];

		public IRelayCommand AddDatabaseConnectionCommand { get; }
		public IRelayCommand<DatabaseConnectionItemViewModel> RemoveDatabaseConnectionCommand { get; }
		public IRelayCommand<DatabaseConnectionItemViewModel> MoveDatabaseConnectionUpCommand { get; }
		public IRelayCommand<DatabaseConnectionItemViewModel> MoveDatabaseConnectionDownCommand { get; }
		public IRelayCommand SetCustomActiveCommand { get; }
		public IRelayCommand<DatabaseConnectionItemViewModel> SetActiveDatabaseConnectionCommand { get; }
		public IRelayCommand<DatabaseConnectionItemViewModel> BrowseSqliteFileCommand { get; }
		public IAsyncRelayCommand TestCustomConnectionCommand { get; }

		/// <summary>
		/// Gets a value indicating whether the custom connection is currently being tested.
		/// </summary>
		public bool IsCustomTesting
		{
			get => _isCustomTesting;
			private set => SetProperty(ref _isCustomTesting, value);
		}

		/// <summary>
		/// Gets the result of the last custom connection test, or <see langword="null"/> if no test has been performed yet.
		/// </summary>
		public bool? CustomTestResult
		{
			get => _customTestResult;
			private set
			{
				if (SetProperty(ref _customTestResult, value))
				{
					RaisePropertyChanged(nameof(CustomTestResultText));
					RaisePropertyChanged(nameof(IsCustomTestSuccessful));
					RaisePropertyChanged(nameof(IsCustomTestFailed));
				}
			}
		}

		/// <summary>
		/// Gets the localized message to display for the last custom connection test result.
		/// </summary>
		public string? CustomTestResultText
		{
			get
			{
				if (CustomTestResult is null)
					return null;
				if (CustomTestResult == true)
					return LocalizationManager.LocalizeStatic("db_test_ok");
				return string.IsNullOrWhiteSpace(_customTestResultMessage)
					? LocalizationManager.LocalizeStatic("db_test_failed")
					: _customTestResultMessage;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the last custom connection test finished successfully.
		/// </summary>
		public bool IsCustomTestSuccessful => CustomTestResult == true;

		/// <summary>
		/// Gets a value indicating whether the last custom connection test failed.
		/// </summary>
		public bool IsCustomTestFailed => CustomTestResult == false;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatDatabaseSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The database settings to edit.</param>
		/// <param name="apiKeyManager">The API key manager used to resolve encrypted connection strings.</param>
		/// <param name="connectionManager">The database connection manager providing the supported connector types.</param>
		/// <param name="connectors">The available database connectors.</param>
		public ChatDatabaseSettingsViewModel(ChatDatabaseSettings settings,
			IApiKeyManagerService apiKeyManager,
			IDatabaseConnectionManager connectionManager,
			IEnumerable<IDatabaseConnector> connectors)
		{
			DatabaseSettings = settings;
			_apiKeyManager = apiKeyManager;
			_connectors = connectors.ToImmutableList();

			_selectedDatabaseConnectionInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.DatabaseConnectionInheritance);
			ConnectorTypes = connectionManager.SupportedConnectors
				.Select(DatabaseConnectorTypeItem.FromValue)
				.ToImmutableList();

			settings.PropertyChanged += DatabaseSettings_PropertyChanged;

			RebuildConnectionItems();

			AddDatabaseConnectionCommand = new RelayCommand(AddDatabaseConnection);
			RemoveDatabaseConnectionCommand = new RelayCommand<DatabaseConnectionItemViewModel>(RemoveDatabaseConnection);
			MoveDatabaseConnectionUpCommand = new RelayCommand<DatabaseConnectionItemViewModel>(MoveDatabaseConnectionUp);
			MoveDatabaseConnectionDownCommand = new RelayCommand<DatabaseConnectionItemViewModel>(MoveDatabaseConnectionDown);
			SetCustomActiveCommand = new RelayCommand(SetCustomActive);
			SetActiveDatabaseConnectionCommand = new RelayCommand<DatabaseConnectionItemViewModel>(SetActiveDatabaseConnection);
			BrowseSqliteFileCommand = new AsyncRelayCommand<DatabaseConnectionItemViewModel>(BrowseSqliteFile);
			TestCustomConnectionCommand = new AsyncRelayCommand(TestCustomConnectionAsync);
		}

		private void DatabaseSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ChatDatabaseSettings.DatabaseConnectionInheritance))
			{
				_selectedDatabaseConnectionInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == DatabaseSettings.DatabaseConnectionInheritance);
				RaisePropertyChanged(nameof(SelectedDatabaseConnectionInheritance));
				RaisePropertyChanged(nameof(EffectiveDatabaseConnection));
				RaisePropertyChanged(nameof(SelectedCustomConnectorTypeItem));
				RebuildConnectionItems();
			}
		}

		private void RebuildConnectionItems()
		{
			foreach (var item in ConnectionItems)
				item.Dispose();
			ConnectionItems.Clear();

			var items = EffectiveDatabaseConnection.Items;
			foreach (var setting in items)
				ConnectionItems.Add(CreateItemViewModel(setting));

			items.CollectionChanged += Items_CollectionChanged;
		}

		private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems is not null)
						foreach (DatabaseConnectionSetting setting in e.NewItems)
							ConnectionItems.Add(CreateItemViewModel(setting));
					break;

				case NotifyCollectionChangedAction.Remove:
					if (e.OldItems is not null)
						foreach (DatabaseConnectionSetting setting in e.OldItems)
						{
							var item = ConnectionItems.FirstOrDefault(i => i.Setting == setting);
							if (item is not null)
							{
								item.Dispose();
								ConnectionItems.Remove(item);
							}
						}
					break;

				case NotifyCollectionChangedAction.Reset:
					foreach (var item in ConnectionItems)
						item.Dispose();
					ConnectionItems.Clear();
					if (sender is RangeObservableCollection<DatabaseConnectionSetting> items)
						foreach (var setting in items)
							ConnectionItems.Add(CreateItemViewModel(setting));
					break;
			}
		}

		private DatabaseConnectionItemViewModel CreateItemViewModel(DatabaseConnectionSetting setting)
		{
			return new DatabaseConnectionItemViewModel(setting, _apiKeyManager, _connectors);
		}

		private void SetCustomActive()
		{
			var connection = EffectiveDatabaseConnection;
			connection.IsCustomActive = true;
			foreach (var item in connection.Items)
				item.IsActive = false;
		}

		private void SetActiveDatabaseConnection(DatabaseConnectionItemViewModel? item)
		{
			if (item is null)
				return;
			var connection = EffectiveDatabaseConnection;
			connection.IsCustomActive = false;
			foreach (var connectionItem in connection.Items)
				connectionItem.IsActive = connectionItem == item.Setting;
		}

		private void AddDatabaseConnection()
		{
			var items = EffectiveDatabaseConnection.Items;
			var setting = new DatabaseConnectionSetting
			{
				Name = LocalizationManager.LocalizeStatic("db_new_connection"),
				ConnectorType = DatabaseConnectorType.SQLite,
				IsEnabled = true,
				IsActive = !items.Any(i => i.IsActive && i.IsEnabled)
			};
			items.Add(setting);
		}

		private void RemoveDatabaseConnection(DatabaseConnectionItemViewModel? item)
		{
			if (item is null)
				return;
			EffectiveDatabaseConnection.Items.Remove(item.Setting);
		}

		private void MoveDatabaseConnectionUp(DatabaseConnectionItemViewModel? item)
		{
			if (item is null)
				return;
			var items = EffectiveDatabaseConnection.Items;
			var index = items.IndexOf(item.Setting);
			if (index > 0)
				items.Move(index, index - 1);
		}

		private void MoveDatabaseConnectionDown(DatabaseConnectionItemViewModel? item)
		{
			if (item is null)
				return;
			var items = EffectiveDatabaseConnection.Items;
			var index = items.IndexOf(item.Setting);
			if (index < items.Count - 1)
				items.Move(index, index + 1);
		}

		private async Task BrowseSqliteFile(DatabaseConnectionItemViewModel? item)
		{
			if (item is null)
				return;

			var result = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = LocalizationManager.LocalizeStatic("db_select_sqlite_file"),
				AllowMultiple = false,
				FileTypeFilter = [new FilePickerFileType("SQLite") { Patterns = ["*.db", "*.sqlite", "*.sqlite3"] }]
			});

			if (result.Count > 0)
				item.Setting.RawConnectionString = $"Data Source={result[0].Path.LocalPath}";
		}

		private async Task TestCustomConnectionAsync(CancellationToken cancellationToken)
		{
			IsCustomTesting = true;
			CustomTestResult = null;
			_customTestResultMessage = null;
			try
			{
				var connection = EffectiveDatabaseConnection;
				if (string.IsNullOrWhiteSpace(connection.CustomConnectionString))
					throw new InvalidOperationException(LocalizationManager.LocalizeStatic("db_test_empty_connection_string"));

				var connector = _connectors.FirstOrDefault(c => c.Type == connection.CustomConnectorType)
					?? throw new InvalidOperationException(string.Format(
						LocalizationManager.LocalizeStatic("db_test_connector_not_found"), connection.CustomConnectorType));

				await using var dbConnection = await connector.ConnectAsync(connection.CustomConnectionString, cancellationToken);
				await dbConnection.TestConnectionAsync(cancellationToken);
				CustomTestResult = true;
			}
			catch (OperationCanceledException)
			{
				CustomTestResult = null;
				_customTestResultMessage = null;
			}
			catch (Exception ex)
			{
				CustomTestResult = false;
				_customTestResultMessage = ex.Message;
			}
			finally
			{
				IsCustomTesting = false;
			}
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				DatabaseSettings.PropertyChanged -= DatabaseSettings_PropertyChanged;
				foreach (var item in ConnectionItems)
					item.Dispose();
			}
		}
	}
}
