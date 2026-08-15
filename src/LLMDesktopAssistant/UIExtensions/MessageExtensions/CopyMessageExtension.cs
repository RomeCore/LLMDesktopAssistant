using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Messages;
using Material.Icons;

namespace LLMDesktopAssistant.UIExtensions.MessageExtensions
{
	[MessageExtension]
	public class CopyMessageExtension : MessageExtension
	{
		public override int Order => 0;

		public CopyMessageExtension(MessageViewModelBase viewModel)
		{
			Icon = MaterialIconKind.ContentCopy;

			Tooltip = "copy_markdown";

			Command = new AsyncRelayCommand(async () =>
			{
				await App.MainTopLevel.Clipboard!.SetTextAsync(viewModel.Message.Content);
			});
		}
	}
}