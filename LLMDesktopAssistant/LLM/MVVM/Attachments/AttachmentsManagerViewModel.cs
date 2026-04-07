using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Utils;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.MVVM.Attachments
{
	public class AttachmentDraftViewModel : ViewModelBase
	{
		private readonly IAttachmentApplicationService _service;

		public string SourceUrl { get; }
		public AttachmentApplicationParameters Parameters { get; }
		public ImmutableList<AttachmentApplicationMode> AvailableModes { get; } =
			Enum.GetValues<AttachmentApplicationMode>().ToImmutableList();

		private bool _isLoading;
		public bool IsLoading
		{
			get => _isLoading;
			set => SetProperty(ref _isLoading, value);
		}

		public AttachmentDraftViewModel(string url, IAttachmentApplicationService service)
		{
			SourceUrl = url;
			_service = service;
			Parameters = new AttachmentApplicationParameters
			{
				SourceUrl = url
			};
		}

		public async Task InitializeAsync()
		{
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

		private void CopyFrom(AttachmentApplicationParameters src)
		{
			Parameters.Mode = src.Mode;
			Parameters.StartLineIndex = src.StartLineIndex;
			Parameters.LineCount = src.LineCount;
			Parameters.StartByteIndex = src.StartByteIndex;
			Parameters.ByteCount = src.ByteCount;
		}
	}

	[ViewModelFor(typeof(AttachmentsManagerView))]
	public class AttachmentsManagerViewModel : ViewModelBase
	{
		public UserInputViewModel Parent { get; }
		public IAttachmentApplicationService ApplicationService { get; }

		private readonly RangeObservableCollection<AttachmentDraftViewModel> _drafts = [];
		public ICollection<AttachmentDraftViewModel> Drafts
		{
			get => _drafts;
			set => _drafts.Reset(value);
		}

		public ICommand ApplyCommand { get; }
		public ICommand RemoveDraftCommand { get; }

		public AttachmentsManagerViewModel(UserInputViewModel parent)
		{
			Parent = parent;
			ApplicationService = parent.Chat.Services.GetRequiredService<IAttachmentApplicationService>();

			ApplyCommand = new AsyncRelayCommand(ApplyAsync);
			RemoveDraftCommand = new RelayCommand<AttachmentDraftViewModel>(d =>
			{
				if (d != null)
					_drafts.Remove(d);
			});
		}

		public async void AcceptDrop(DragEventArgs args)
		{
			if (args.Data.GetData(DataFormats.FileDrop) is string[] files)
			{
				foreach (var file in files)
				{
					var draft = new AttachmentDraftViewModel(file, ApplicationService);
					_drafts.Add(draft);

					_ = draft.InitializeAsync(); // fire & forget
				}
			}

			if (args.Data.GetData(DataFormats.Text) is string text)
			{
				var draft = new AttachmentDraftViewModel(text, ApplicationService);
				_drafts.Add(draft);
				_ = draft.InitializeAsync();
			}
		}

		private async Task ApplyAsync()
		{
			foreach (var draft in _drafts.ToList())
			{
				var attachment = await ApplicationService.ApplicateAttachmentAsync(draft.Parameters);
				Parent.Attachments.Add(new AttachmentViewModel(Parent, attachment));
			}

			_drafts.Clear();
			DialogHost.CloseDialogCommand.Execute(null, null);
		}
	}
}