using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.LLM.MVVM.Additional.Context;
using LLMDesktopAssistant.LLM.Services;
using Material.Icons;
using Serilog;

namespace LLMDesktopAssistant.UIExtensions.MessageExtensions
{
	/// <summary>
	/// Adds a button to summarize the message with all previous messages.
	/// If a summary already exists, removes it.
	/// </summary>
	[MessageExtension(Targets = MessageExtensionTargets.Both)]
	public class SummarizeMessageExtension : MessageExtension
	{
		public override int Order => 51;

		public SummarizeMessageExtension(MessageViewModelBase viewModel)
		{
			Icon = MaterialIconKind.TextBoxSearchOutline;

			Command = new AsyncRelayCommand(async () =>
			{
				try
				{
					var viewModels = viewModel.Message.AdditionalViewModels;
					var existing = viewModels.TryGet<SummaryViewModel>();
					if (existing != null)
					{
						viewModels.Remove(existing);
						return;
					}

					var summarizer = viewModel.ChatViewModel.Chat.Services.GetRequiredService<IChatSummarizationService>();
					await summarizer.SummarizeMessageWithPreviousMessagesAsync(viewModel.Message);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to summarize message: {Error}", ex);
				}
			});
		}
	}
}
