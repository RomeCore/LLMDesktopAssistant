using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Controls.Toasts
{
	/// <summary>
	/// Represents an exception that has been already shown as a toast message.
	/// </summary>
	public class ToastedException : Exception
	{
		public string Title { get; }

		public ToastedException(string title, string message) : base(message)
		{
			Title = title;
		}

		public ToastedException(string title, string message, Exception? innerException) : base(message, innerException)
		{
			Title = title;
		}
	}
}