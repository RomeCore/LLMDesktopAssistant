using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.Avalonia.Behaviours.CodeBlockExtensions;
using LLMDesktopAssistant.Core.Services;
using LLMDesktopAssistant.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LLMDesktopAssistant.Avalonia.Behaviours
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
				var extensions = new AvaloniaList<CodeBlockExtension>();
				extensions.AddRange(_codeBlockExtensionsTypes
					.Instantiate<CodeBlockExtension>(ServiceRegistry.Provider, block)
					.OrderBy(ext => ext.Order));
				SetCodeBlockExtensions(block, extensions);
			}
			else
			{
				var extensions = GetCodeBlockExtensions(block);
				foreach (var extension in extensions ?? [])
					extension.Dispose();
				extensions?.Clear();
			}
		}
	}
}