using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	}
}