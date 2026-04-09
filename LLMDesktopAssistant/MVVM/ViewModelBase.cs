using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LLMDesktopAssistant.MVVM
{
	/// <summary>
	/// Base class for view models. Implements INotifyPropertyChanged to facilitate data binding.
	/// </summary>
	public class ViewModelBase : NotifyPropertyChanged
	{
		protected static void InvokeUI(Action action)
		{
			App.Current.Dispatcher.Invoke(action);
		}

		protected static DispatcherOperation BeginInvokeUI(Action action)
		{
			return App.Current.Dispatcher.BeginInvoke(action);
		}

		protected static DispatcherOperation BeginInvokeUI(Func<Task> action)
		{
			return App.Current.Dispatcher.BeginInvoke(action);
		}
	}
}