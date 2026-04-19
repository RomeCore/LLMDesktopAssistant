using Avalonia.Threading;
using LLMDesktopAssistant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.MVVM
{
	/// <summary>
	/// Base class for view models. Implements INotifyPropertyChanged to facilitate data binding.
	/// </summary>
	public class ViewModelBase : NotifyPropertyChanged
	{
		protected static void InvokeUI(Action action, CancellationToken cancellationToken = default)
		{
			Dispatcher.UIThread.Invoke(action, DispatcherPriority.Default, cancellationToken);
		}

		protected static DispatcherOperation InvokeUIAsync(Action action, CancellationToken cancellationToken = default)
		{
			return Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Default, cancellationToken);
		}
	}
}