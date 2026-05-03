using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.LLM.MVVM.Additional.Context;
using Material.Icons;

namespace LLMDesktopAssistant.UIExtensions.MessageExtensions
{
	/// <summary>
	/// Adds a button to toggle the context shield on the message.
	/// The context shield prevents further messages from being included into the context.
	/// </summary>
	[MessageExtension(Targets = MessageExtensionTargets.Assistant)]
	public class ContextShieldMessageExtension : MessageExtension
	{
		public override MaterialIconKind Icon => MaterialIconKind.ShieldOutline;

		public override ICommand Command { get; }

		public override int Order => 50;

		public ContextShieldMessageExtension(MessageViewModelBase viewModel)
		{
			Command = new RelayCommand(() =>
			{
				var viewModels = viewModel.Message.AdditionalViewModels;
				var existing = viewModels.TryGet<ContextShieldViewModel>();
				if (existing != null)
				{
					viewModels.Remove(existing);
				}
				else
				{
					viewModels.TryReplace(new ContextShieldViewModel());
				}
			});
		}
	}
}
