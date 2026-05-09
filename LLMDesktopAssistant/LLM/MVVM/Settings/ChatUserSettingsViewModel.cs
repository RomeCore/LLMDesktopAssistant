using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.WebUI;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace LLMDesktopAssistant.LLM.Settings
{
	[ViewModelFor(typeof(ChatUserSettingsView))]
	public class ChatUserSettingsViewModel : ViewModelBase
	{
		public RangeObservableCollection<UserInformation> Users { get; }

		private UserInformation? _selectedUser;
		public UserInformation? SelectedUser
		{
			get => _selectedUser;
			set
			{
				if (SetProperty(ref _selectedUser, value))
				{
					DeleteCommand.NotifyCanExecuteChanged();
					LoadProfileImage(value);
				}
			}
		}

		private Bitmap? _profileImage;
		public Bitmap? ProfileImage
		{
			get => _profileImage;
			private set => SetProperty(ref _profileImage, value);
		}

		public ChatUserSettingsViewModel(RangeObservableCollection<UserInformation> users)
		{
			Users = users;

			LoadProfileImages();
			foreach (var user in users)
				user.PropertyChanged += (_, _) => LoadProfileImage(user);
		}

		private void LoadProfileImages()
		{
			LoadProfileImage(SelectedUser);
		}

		private void LoadProfileImage(UserInformation? user)
		{
			try
			{
				if (user == null || string.IsNullOrWhiteSpace(user.Base64ProfileImage))
				{
					ProfileImage = null;
					return;
				}

				var bytes = Convert.FromBase64String(user.Base64ProfileImage);
				using var ms = new MemoryStream(bytes);
				ProfileImage = new Bitmap(ms);
			}
			catch
			{
				ProfileImage = null;
			}
		}

		private IRelayCommand? _addCommand;
		public IRelayCommand AddCommand => _addCommand ??= new RelayCommand(() =>
		{
			var newUser = new UserInformation
			{
				Login = "new_user",
				Name = "New User"
			};
			newUser.PropertyChanged += (_, _) => LoadProfileImage(SelectedUser);
			Users.Add(newUser);
			SelectedUser = Users.LastOrDefault();
		});

		private IRelayCommand? _deleteCommand;
		public IRelayCommand DeleteCommand => _deleteCommand ??= new RelayCommand(() =>
		{
			if (SelectedUser == null || Users.Count <= 1)
				return;

			var index = Users.IndexOf(SelectedUser);
			Users.Remove(SelectedUser);
			SelectedUser = index > 0 && index < Users.Count
				? Users[index]
				: Users.LastOrDefault();
		}, () => SelectedUser != null);

		private IAsyncRelayCommand? _selectImageCommand;
		public IAsyncRelayCommand SelectImageCommand => _selectImageCommand ??= new AsyncRelayCommand(async () =>
		{
			if (SelectedUser == null)
				return;

			var files = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = LocalizationManager.LocalizeStatic("user_select_image_title"),
				FileTypeFilter =
				[
					new("Image files")
					{
						Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp"]
					}
				],
				AllowMultiple = false
			});

			var file = files?.FirstOrDefault();
			if (file == null)
				return;

			try
			{
				await using var stream = await file.OpenReadAsync();
				using var image = await Image.LoadAsync(stream);
				image.Mutate(x => x.Resize(new ResizeOptions
				{
					Size = new Size(128, 128),
					Mode = ResizeMode.Crop
				}));

				using var ms = new MemoryStream();
				await image.SaveAsync(ms, PngFormat.Instance);
				SelectedUser.Base64ProfileImage = Convert.ToBase64String(ms.ToArray());
				LoadProfileImage(SelectedUser);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
			}
		});

		private IRelayCommand? _clearImageCommand;
		public IRelayCommand ClearImageCommand => _clearImageCommand ??= new RelayCommand(() =>
		{
			if (SelectedUser == null)
				return;

			SelectedUser.Base64ProfileImage = string.Empty;
			ProfileImage = null;
		});
	}
}
