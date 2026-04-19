using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Utils
{
	public static class AvaloniaTreeExtensions
	{
		public static T? FindParent<T>(this StyledElement element)
			where T : StyledElement
		{
			var current = element.Parent;
			while (current != null)
			{
				if (current is T result)
					return result;
				current = current.Parent;
			}
			return null;
		}
	}
}