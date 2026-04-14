using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.Services
{
	/// <summary>
	/// Represents a service that can be dynamically created and replaced with another service within its .
	/// </summary>
	public interface IDynamicService
	{
		/// <summary>
		/// Initializes the service. This method is called once, after created.
		/// </summary>
		public void Initialize()
		{
		}

		/// <summary>
		/// Shuts down the service. This method is called once, before the application shuts down or when a new service replaces this one.
		/// </summary>
		public void Shutdown()
		{
		}
	}
}