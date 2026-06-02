using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Services
{
	/// <summary>
	/// Abstract base class for configuring services in an application.
	/// </summary>
	public abstract class ServiceConfigurator
	{
		/// <summary>
		/// Configures the services for the application. This method should be overridden by subclasses to add specific service configurations.
		/// </summary>
		/// <param name="services">The collection of services to configure.</param>
		public abstract void Configure(IServiceCollection services);
	}
}