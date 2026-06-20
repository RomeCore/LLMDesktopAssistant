using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.ApiKeys
{
	/// <summary>
	/// ViewModel for the "Add / Edit API Key" dialog.
	/// </summary>
	public class AddApiKeyDialogViewModel : ViewModelBase
	{
		private readonly IApiKeyManagerService _apiKeys;

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string _value = string.Empty;
		public string Value
		{
			get => _value;
			set => SetProperty(ref _value, value);
		}

		private ApiKeyStorageScheme _scheme = ApiKeyStorageScheme.Encrypted;
		public ApiKeyStorageScheme Scheme
		{
			get => _scheme;
			set => SetProperty(ref _scheme, value);
		}

		private string? _errorMessage;
		public string? ErrorMessage
		{
			get => _errorMessage;
			set => SetProperty(ref _errorMessage, value);
		}

		/// <summary>
		/// If set, the dialog is in edit mode and will update this key instead of creating a new one.
		/// </summary>
		public Guid? EditingKeyId { get; set; }

		/// <summary>
		/// Whether the dialog is in edit mode.
		/// </summary>
		public bool IsEditMode => EditingKeyId != null;

		/// <summary>
		/// The title text for the dialog.
		/// </summary>
		public string TitleText => IsEditMode
			? LocalizationManager.LocalizeStatic("settings_apikey_edit_title")
			: LocalizationManager.LocalizeStatic("settings_apikey_add_title");

		/// <summary>
		/// The confirm button text.
		/// </summary>
		public string ConfirmButtonText => IsEditMode
			? LocalizationManager.LocalizeStatic("save")
			: LocalizationManager.LocalizeStatic("add");

		/// <summary>
		/// The ID of the created/updated key, set after successful operation.
		/// </summary>
		public Guid? CreatedKeyId { get; private set; }

		public ICommand AddCommand { get; }
		public ICommand CancelCommand { get; }

		public AddApiKeyDialogViewModel()
		{
			_apiKeys = ServiceRegistry.Provider.GetRequiredService<IApiKeyManagerService>();

			AddCommand = new RelayCommand(Add);
			CancelCommand = new RelayCommand(Cancel);
		}

		private void Add()
		{
			ErrorMessage = null;

			if (string.IsNullOrWhiteSpace(Name))
			{
				ErrorMessage = LocalizationManager.LocalizeStatic("settings_apikey_error_name_required");
				return;
			}

			if (string.IsNullOrEmpty(Value))
			{
				ErrorMessage = LocalizationManager.LocalizeStatic("settings_apikey_error_value_required");
				return;
			}

			try
			{
if (EditingKeyId != null)
				{
					// Edit mode — update existing key
					_apiKeys.UpdateKey(EditingKeyId.Value, Name.Trim(), Value, Scheme);
					CreatedKeyId = EditingKeyId;
				}
				else
				{
					// Add mode — create new key
					var item = _apiKeys.AddKey(Name.Trim(), Value, Scheme);
					CreatedKeyId = item.Id;
				}

				DialogHostAvalonia.DialogHost.Close(null, true);
			}
			catch (Exception ex)
			{
				ErrorMessage = ex.Message;
			}
		}

		private void Cancel()
		{
			DialogHostAvalonia.DialogHost.Close(null, false);
		}
	}
}
