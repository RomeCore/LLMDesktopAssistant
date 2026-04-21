using Material.Icons;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.UIExtensions.MessageExtensions
{
	/// <summary>
	/// Represents an extension for a message, which can be used to add additional functionality or UI elements to messages.
	/// </summary>
	public abstract class MessageExtension : NotifyPropertyChanged
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

		private bool _isButtonVisible = true;
		/// <summary>
		/// The visibility of the extension. If false, the extension will not be displayed.
		/// </summary>
		public bool IsButtonVisible
		{
			get => _isButtonVisible;
			protected set => SetProperty(ref _isButtonVisible, value);
		}
	}
}
