using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.Modules
{
	/// <summary>
	/// Represents a module that can be dynamically created and replaced with another module.
	/// </summary>
	public interface IDynamicModule
	{
		/// <summary>
		/// Initializes the module. This method is called once, after created.
		/// </summary>
		public void Initialize()
		{
		}

		/// <summary>
		/// Shuts down the module. This method is called once, before the application shuts down or when a new module replaces this one.
		/// </summary>
		public void Shutdown()
		{
		}
	}
}