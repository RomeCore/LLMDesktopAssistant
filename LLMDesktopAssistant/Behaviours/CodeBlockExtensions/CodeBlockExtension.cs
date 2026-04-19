using LiveMarkdown.Avalonia;
using LLMDesktopAssistant;
using Material.Icons;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Behaviours.CodeBlockExtensions
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
		/// The icon for button associated with this extension.
		/// </summary>
		public abstract MaterialIconKind Icon { get; }

		/// <summary>
		/// The command for button associated with this extension.
		/// </summary>
		public abstract ICommand Command { get; }

		private bool _isVisible = true;
		/// <summary>
		/// The visibility of the extension. If false, the extension will not be displayed.
		/// </summary>
		public bool IsVisible
		{
			get => _isVisible;
			protected set => SetProperty(ref  _isVisible, value);
		}

		private object? _additionalViewModel;
		/// <summary>
		/// The view model that will be appended to the end of the code block.
		/// </summary>
		public object? AdditionalViewModel
		{
			get => _additionalViewModel;
			protected set => SetProperty(ref _additionalViewModel, value);
		}
	}
}