using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.Utils;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using UglyToad.PdfPig.Logging;

namespace LLMDesktopAssistant.LLM.Attachments
{
	public class AttachmentApplicationModeViewModel
	{
		public required string Title { get; init; }
		public required AttachmentApplicationMode Mode { get; init; }
		public bool HasLineSelections { get; init; } = false;
		public bool HasByteSelections { get; init; } = false;
	}

	public class AttachmentDraftViewModel : ViewModelBase
	{
		private readonly IAttachmentApplicationService _service;

		public AttachmentsManagerViewModel Manager { get; }
		public Uri SourceUri { get; }
		public string? Title { get; }
		public AttachmentApplicationParameters Parameters { get; }

		public ImmutableList<AttachmentApplicationModeViewModel> AvailableModes { get; } =
			[
				new AttachmentApplicationModeViewModel
				{
					Title = "application-only_reference",
					Mode = AttachmentApplicationMode.OnlyReference
				},
				new AttachmentApplicationModeViewModel
				{
					Title = "application-full_contents",
					Mode = AttachmentApplicationMode.FullContents
				},
				new AttachmentApplicationModeViewModel
				{
					Title = "application-partial_contents",
					Mode = AttachmentApplicationMode.PartialContents,
					HasLineSelections = true
				},
				new AttachmentApplicationModeViewModel
				{
					Title = "application-full_hexadecimal",
					Mode = AttachmentApplicationMode.FullHexadecimal
				},
				new AttachmentApplicationModeViewModel
				{
					Title = "application-hexadecimal_partial",
					Mode = AttachmentApplicationMode.HexadecimalPartial,
					HasByteSelections = true
				},
				new AttachmentApplicationModeViewModel
				{
					Title = "application-description",
					Mode = AttachmentApplicationMode.Description
				}
			];

		public AttachmentApplicationModeViewModel SelectedMode
		{
			get => AvailableModes.FirstOrDefault(x => x.Mode == Parameters.Mode, AvailableModes[0]);
			set
			{
				if (value.Mode != Parameters.Mode)
				{
					Parameters.Mode = value.Mode;
					RaisePropertyChanged(nameof(SelectedMode));
				}
			}
		}

		private bool _isLoading;
		public bool IsLoading
		{
			get => _isLoading;
			set => SetProperty(ref _isLoading, value);
		}

		public ICommand ApplyCommand { get; }
		public ICommand RemoveCommand { get; }

		public AttachmentDraftViewModel(AttachmentsManagerViewModel parent,
			Uri uri, IAttachmentApplicationService service)
		{
			Manager = parent;
			SourceUri = uri;
			Title = Path.GetFileName(uri.LocalPath);
			_service = service;
			Parameters = new AttachmentApplicationParameters
			{
				SourceUri = uri
			};

			ApplyCommand = new AsyncRelayCommand(ApplyAsync);
			RemoveCommand = new RelayCommand(Remove);
		}

		public async Task InitializeAsync()
		{
			// Just return without recommended parameters.
			if (!IsLoading)
				return;

			IsLoading = true;
			try
			{
				var recommended = await _service.GetRecommendedParamatersAsync(SourceUri);
				CopyFrom(recommended);
			}
			finally
			{
				IsLoading = false;
			}
		}

		private async Task ApplyAsync()
		{
			var attachment = await Manager.ApplicationService.ApplicateAttachmentAsync(Parameters);
			Manager.UserInput.Attachments.Add(new AttachmentViewModel(Manager.UserInput, attachment));
			Manager.Drafts.Remove(this);
		}

		private void Remove()
		{
			Manager.Drafts.Remove(this);
		}

		private void CopyFrom(AttachmentApplicationParameters src)
		{
			Parameters.Mode = src.Mode;
			Parameters.StartLine = src.StartLine;
			Parameters.EndLine = src.EndLine;
			Parameters.StartByte = src.StartByte;
			Parameters.EndByte = src.EndByte;
		}
	}

	[ViewModelFor(typeof(AttachmentsManagerView))]
	public class AttachmentsManagerViewModel : ViewModelBase
	{
		public UserInputViewModel UserInput { get; }
		public IAttachmentApplicationService ApplicationService { get; }

		private readonly AvaloniaList<AttachmentDraftViewModel> _drafts = [];
		public ICollection<AttachmentDraftViewModel> Drafts
		{
			get => _drafts;
			set
			{
				_drafts.Clear();
				_drafts.AddRange(value);
			}
		}

		private string _inputUrl = string.Empty;
		public string InputUrl
		{
			get => _inputUrl;
			set => SetProperty(ref _inputUrl, value);
		}

		public ICommand AddUrlCommand { get; }
		public ICommand AttachFilesCommand { get; }

		public AttachmentsManagerViewModel(UserInputViewModel parent)
		{
			UserInput = parent;
			ApplicationService = parent.Chat.Services.GetRequiredService<IAttachmentApplicationService>();

			AddUrlCommand = new RelayCommand(AddUrl);
			AttachFilesCommand = new AsyncRelayCommand(AttachFiles);

			_drafts.CollectionChanged += (s, e) =>
			{
				// Close the dialog when there are no drafts left.
				if (Drafts.Count == 0)
				{
					// TODO: Uncomment
					// DialogHost.Close(null);
				}
			};
		}

		private void AddUrl()
		{
			var url = InputUrl?.Trim();
			if (string.IsNullOrEmpty(url))
				return;

			var draft = new AttachmentDraftViewModel(this, new Uri(url), ApplicationService);
			_drafts.Add(draft);
			_ = draft.InitializeAsync();

			InputUrl = string.Empty;
		}

		private async Task AttachFiles()
		{
			var result = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = Locale.select_working_directory,
				FileTypeFilter = [
					new FilePickerFileType("All files (*.*)")
				],
				AllowMultiple = false
			});

			if (result.Count > 0)
			{
				foreach (var file in result)
				{
					var draft = new AttachmentDraftViewModel(this, file.Path, ApplicationService);
					_drafts.Add(draft);
					_ = draft.InitializeAsync();
				}
			}
		}

		public async void AcceptDrop(DragEventArgs args)
		{
			if (args.DataTransfer.TryGetFiles() is IStorageItem[] files)
			{
				foreach (var file in files)
				{
					var draft = new AttachmentDraftViewModel(this, file.Path, ApplicationService);
					_drafts.Add(draft);

					_ = draft.InitializeAsync(); // fire & forget
				}
			}

			if (args.DataTransfer.TryGetText() is string text)
			{
				var draft = new AttachmentDraftViewModel(this, new Uri(text), ApplicationService);
				_drafts.Add(draft);
				_ = draft.InitializeAsync();
			}
		}
	}
}