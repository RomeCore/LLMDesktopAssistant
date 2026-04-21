using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Messages;
using Material.Icons;

namespace LLMDesktopAssistant.UIExtensions.MessageExtensions
{
	[MessageExtension]
	public class CopyMessageExtension : MessageExtension
	{
		public override MaterialIconKind Icon => MaterialIconKind.ContentCopy;

		public override ICommand Command { get; }

		public CopyMessageExtension(MessageViewModelBase viewModel)
		{
			Command = new AsyncRelayCommand(async () =>
			{
				await App.MainTopLevel.Clipboard!.SetTextAsync(viewModel.Message.Content);
			});
		}
	}
}