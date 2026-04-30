using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.Attachments
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