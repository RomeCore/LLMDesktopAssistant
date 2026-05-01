using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	[ViewModelFor(typeof(AgentInfoSettingsView))]
	public class AgentInfoSettingsViewModel : ViewModelBase
	{
		public AgentInformation AgentInfo { get; }

		private Bitmap? _profileImage;
		public Bitmap? ProfileImage
		{
			get => _profileImage;
			private set => SetProperty(ref _profileImage, value);
		}

		public IAsyncRelayCommand SelectImageCommand { get; }
		public IRelayCommand ClearImageCommand { get; }

		public AgentInfoSettingsViewModel(AgentInformation agentInfo)
		{
			AgentInfo = agentInfo;

			// Load existing profile image if any
			LoadProfileImage();

			// Subscribe to changes from the text field
			AgentInfo.PropertyChanged += (_, _) => LoadProfileImage();

			SelectImageCommand = new AsyncRelayCommand(async () =>
			{
				var files = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
				{
					Title = "Select agent profile image",
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
					AgentInfo.Base64ProfileImage = Convert.ToBase64String(ms.ToArray());
					LoadProfileImage();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
				}
			});

			ClearImageCommand = new RelayCommand(() =>
			{
				AgentInfo.Base64ProfileImage = string.Empty;
				ProfileImage = null;
			});
		}

		private void LoadProfileImage()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(AgentInfo.Base64ProfileImage))
				{
					ProfileImage = null;
					return;
				}

				var bytes = Convert.FromBase64String(AgentInfo.Base64ProfileImage);
				using var ms = new MemoryStream(bytes);
				ProfileImage = new Bitmap(ms);
			}
			catch
			{
				ProfileImage = null;
			}
		}
	}
}
