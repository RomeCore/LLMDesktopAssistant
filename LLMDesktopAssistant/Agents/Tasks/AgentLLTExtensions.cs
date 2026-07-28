using System;
using System.Collections.Generic;
using System.Text;
using DocumentFormat.OpenXml.InkML;
using LLTSharp;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public static class AgentLLTExtensions
	{
		/// <summary>
		/// Renders a Agent messages from a LLTSharp template and context.
		/// </summary>
		/// <param name="template">The LLTSharp template to render.</param>
		/// <param name="context">The context to use for rendering the template.</param>
		/// <returns>A list of Agent messages.</returns>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static IEnumerable<AgentChatMessage> RenderToAgent(this IMessagesTemplate template, object? context)
		{
			var rendered = template.Render(context);
			return rendered.ToAgent();
		}

		/// <summary>
		/// Converts a list of LLTSharp messages to Agent messages.
		/// </summary>
		/// <param name="messages">The list of LLTSharp messages to convert.</param>
		/// <returns>A list of Agent messages.</returns>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static IEnumerable<AgentChatMessage> ToAgent(this IEnumerable<Message> messages)
		{
			return messages.Select(m => m.Role switch
			{
				Role.System => (AgentChatMessage)new AgentSystemMessage { Content = m.Content },
				Role.User => new AgentUserMessage { Content = m.Content },
				Role.Assistant => new AgentAssistantMessage { Content = m.Content },
				Role.Tool => throw new NotSupportedException("Tool messages are not supported currently."),
				_ => throw new ArgumentOutOfRangeException(nameof(m.Role), $"Unknown role: {m.Role}"),
			});
		}
	}
}
