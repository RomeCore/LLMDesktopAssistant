using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.LLM.Services.Attachments;
using LLMDesktopAssistant.Core.Utils;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LLMDesktopAssistant.Avalonia.LLM.Attachments
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
		public string SourceUrl { get; }
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
			string url, IAttachmentApplicationService service)
		{
			Manager = parent;
			SourceUrl = url;
			Title = Path.GetFileName(url);
			_service = service;
			Parameters = new AttachmentApplicationParameters
			{
				SourceUrl = url
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
				var recommended = await _service.GetRecommendedParamatersAsync(SourceUrl);
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

		private readonly RangeObservableCollection<AttachmentDraftViewModel> _drafts = [];
		public ICollection<AttachmentDraftViewModel> Drafts
		{
			get => _drafts;
			set => _drafts.Reset(value);
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
			AttachFilesCommand = new RelayCommand(AttachFiles);

			_drafts.CollectionChanged += (s, e) =>
			{
				// Close the dialog when there are no drafts left.
				if (Drafts.Count == 0)
				{
					DialogHost.Close(null);
				}
			};
		}

		private void AddUrl()
		{
			var url = InputUrl?.Trim();
			if (string.IsNullOrEmpty(url))
				return;

			var draft = new AttachmentDraftViewModel(this, url, ApplicationService);
			_drafts.Add(draft);
			_ = draft.InitializeAsync();

			InputUrl = string.Empty;
		}

		private void AttachFiles()
		{
			App.Current.Se

			var dialog = new System.Windows.Forms.OpenFileDialog
			{
				Title = "Select files to attach",
				Multiselect = true,
				Filter = "All files (*.*)|*.*"
			};

			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				foreach (var file in dialog.FileNames)
				{
					var draft = new AttachmentDraftViewModel(this, file, ApplicationService);
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
					var draft = new AttachmentDraftViewModel(this, file.Path.AbsoluteUri, ApplicationService);
					_drafts.Add(draft);

					_ = draft.InitializeAsync(); // fire & forget
				}
			}

			if (args.DataTransfer.TryGetText() is string text)
			{
				var draft = new AttachmentDraftViewModel(this, text, ApplicationService);
				_drafts.Add(draft);
				_ = draft.InitializeAsync();
			}
		}
	}
}