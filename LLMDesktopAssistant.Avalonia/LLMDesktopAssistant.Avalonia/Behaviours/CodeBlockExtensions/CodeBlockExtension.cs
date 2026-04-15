using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.Core;
using Material.Icons;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Avalonia.Behaviours.CodeBlockExtensions
{
	/// <summary>
	/// Base class for markdown code block extensions.
	/// </summary>
	public abstract class CodeBlockExtension : NotifyPropertyChanged
	{
		/// <summary>
		/// The order of the extension. Lower values are displayed first in the StackPanel.
		/// </summary>
		public virtual int Order => 0;

		/// <summary>
		/// The visibility of the extension. If false, the extension will not be displayed.
		/// </summary>
		public virtual bool IsVisible => true;

		/// <summary>
		/// The icon for button associated with this extension.
		/// </summary>
		public abstract MaterialIconKind Icon { get; }

		/// <summary>
		/// The command for button associated with this extension.
		/// </summary>
		public abstract ICommand Command { get; }

		/// <summary>
		/// The view model that will be appended to the end of the code block.
		/// </summary>
		public virtual object? AdditionalViewModel => null;
	}
}