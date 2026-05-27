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
		private readonly IChatOperationService _chatOperator;
		private readonly IUserManagementService _userManager;

		public QuickActionService(IChatOperationService chatOperator, IUserManagementService userManager)
		{
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

		private void OnActionClicked(string action)
		{
			_ = _chatOperator.SendUserInputAsync(new UserInput
			{
				Content = action,
				SenderLogin = _userManager.GetLocalUsers().FirstOrDefault()?.Login ?? "user"
			}, generate: true);
		}
	}
}