using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Core.LLM.Domain;
using System.Windows.Input;

namespace LLMDesktopAssistant.Avalonia.LLM.Attachments
{
	[ViewModelFor(typeof(AttachmentView))]
	public class AttachmentViewModel : ViewModelBase
	{
		public UserInputViewModel Parent { get; }
		public Attachment Attachment { get; }

		public ICommand RemoveAttachmentCommand { get; }

		public AttachmentViewModel(UserInputViewModel parent, Attachment attachment)
		{
			Parent = parent;
			Attachment = attachment;

			RemoveAttachmentCommand = new RelayCommand(() =>
			{
				Parent.Attachments.Remove(this);
			});
		}
	}
}