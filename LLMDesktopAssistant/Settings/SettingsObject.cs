using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Settings
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class SettingsObjectAttribute : Attribute
	{
		public string Id { get; }

		public SettingsObjectAttribute(string id)
		{
			Id = id;
		}
	}

	public abstract class SettingsObject : NotifyPropertyChanged
	{
	}
}