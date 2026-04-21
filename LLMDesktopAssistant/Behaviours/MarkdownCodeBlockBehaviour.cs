using Avalonia;
using Avalonia.Collections;
using Avalonia.VisualTree;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.LLM;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.UIExtensions.CodeBlockExtensions;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Behaviours
{
	public static class MarkdownCodeBlockBehaviour
	{
		/// <summary>
		/// The attached property for giving some extensional behavior to code blocks, such as running code and saving it.
		/// </summary>
		public static readonly AttachedProperty<bool> IsExtendedCodeBlockProperty =
			AvaloniaProperty.RegisterAttached<CodeBlock, bool>(
				"IsExtendedCodeBlock",
				typeof(MarkdownCodeBlockBehaviour),
				false);

		public static readonly AttachedProperty<AvaloniaList<CodeBlockExtension>?> CodeBlockExtensionsProperty =
			AvaloniaProperty.RegisterAttached<CodeBlock, AvaloniaList<CodeBlockExtension>?>(
				"CodeBlockExtensions",
				typeof(MarkdownCodeBlockBehaviour),
				null);

		public static void SetIsExtendedCodeBlock(CodeBlock element, bool value)
		{
			element.SetValue(IsExtendedCodeBlockProperty, value);
		}

		public static bool GetIsExtendedCodeBlock(CodeBlock element)
		{
			return element.GetValue(IsExtendedCodeBlockProperty);
		}

		public static void SetCodeBlockExtensions(CodeBlock element, AvaloniaList<CodeBlockExtension>? value)
		{
			element.SetValue(CodeBlockExtensionsProperty, value);
		}

		public static AvaloniaList<CodeBlockExtension>? GetCodeBlockExtensions(CodeBlock element)
		{
			return element.GetValue(CodeBlockExtensionsProperty);
		}

		private static readonly ImmutableList<Type> _codeBlockExtensionsTypes;

		static MarkdownCodeBlockBehaviour()
		{
			IsExtendedCodeBlockProperty.Changed.AddClassHandler<CodeBlock, bool>(
				(o, e) => IsExtendedChanged(o, e.GetNewValue<bool>()));

			_codeBlockExtensionsTypes = ReflectionUtility
				.GetTypesWithAttribute<CodeBlockExtension, CodeBlockExtensionAttribute>()
				.Select(t => t.Type).ToImmutableList();
		}

		private static void IsExtendedChanged(CodeBlock block, bool isExtended)
		{
			if (isExtended)
			{
				if (block.IsAttachedToVisualTree())
				{
					AttachExtensions(block);
				}
				else
				{
					block.AttachedToVisualTree += (s, e) =>
					{
						AttachExtensions(block);
					};
				}
			}
			else
			{
				var extensions = GetCodeBlockExtensions(block);
				foreach (var extension in extensions ?? [])
					extension.Dispose();
				extensions?.Clear();
			}
		}

		private static void AttachExtensions(CodeBlock block)
		{
			var chatView = block.FindParent<ChatView>();
			var chatViewModel = chatView?.DataContext as ChatViewModel;
			var services = chatViewModel?.Chat.Services ?? ServiceRegistry.Provider;

			var extensions = new AvaloniaList<CodeBlockExtension>();
			extensions.AddRange(_codeBlockExtensionsTypes
				.Instantiate<CodeBlockExtension>(services, block)
				.OrderBy(ext => ext.Order));

			var oldExtensions = GetCodeBlockExtensions(block);
			foreach (var extension in oldExtensions ?? [])
				extension.Dispose();
			oldExtensions?.Clear();

			SetCodeBlockExtensions(block, extensions);
		}
	}
}