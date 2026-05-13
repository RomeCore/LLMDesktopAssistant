using LLTSharp;
using RCLargeLanguageModels.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Utils
{
	public static class LLTExtensions
	{
		/// <summary>
		/// Renders a RCLLM messages from a LLTSharp template and context.
		/// </summary>
		/// <param name="template">The LLTSharp template to render.</param>
		/// <param name="context">The context to use for rendering the template.</param>
		/// <returns>A list of RCLLM messages.</returns>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static IEnumerable<IMessage> RenderRCLLM(this IMessagesTemplate template, object? context)
		{
			var rendered = template.Render(context);

			return rendered.ToRCLLM();
		}

		/// <summary>
		/// Converts a list of LLTSharp messages to RCLLM messages.
		/// </summary>
		/// <param name="messages">The list of LLTSharp messages to convert.</param>
		/// <returns>A list of RCLLM messages.</returns>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static IEnumerable<IMessage> ToRCLLM(this IEnumerable<LLTSharp.Message> messages)
		{
			return messages.Select(m => m.Role switch
			{
				LLTSharp.Role.System => (IMessage)new SystemMessage(m.Content),
				LLTSharp.Role.User => new UserMessage(m.Content),
				LLTSharp.Role.Assistant => new AssistantMessage(m.Content),
				LLTSharp.Role.Tool => throw new NotSupportedException("Tool messages are not supported currently."),
				_ => throw new ArgumentOutOfRangeException(nameof(m.Role), $"Unknown role: {m.Role}"),
			});
		}
	}
}