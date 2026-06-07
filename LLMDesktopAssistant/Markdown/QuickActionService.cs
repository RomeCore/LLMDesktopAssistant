using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Markdown.UINodes;
using LLMDesktopAssistant.WebUI;

namespace LLMDesktopAssistant.Markdown
{
	[ChatService]
	public class QuickActionService : Disposable
	{
		private readonly Chat _chat;
		private readonly IChatOperationService _chatOperator;
		private readonly IUserManagementService _userManager;

		public QuickActionService(Chat chat, IChatOperationService chatOperator, IUserManagementService userManager)
		{
			_chat = chat;
			_chatOperator = chatOperator;
			_userManager = userManager;

			QuickActionUiNode.OnActionClicked += OnActionClicked;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				QuickActionUiNode.OnActionClicked -= OnActionClicked;
		}

		private void OnActionClicked(string action, Chat chat)
		{
			if (_chat != chat)
				return;

			_ = _chatOperator.SendUserInputAsync(new UserInput
			{
				Content = action,
				SenderLogin = _userManager.GetLocalUsers().FirstOrDefault()?.Login ?? "user"
			}, generate: true);
		}
	}
}