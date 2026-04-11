using System.Windows;

namespace LLMDesktopAssistant.Core.Modules
{
	/// <summary>
	/// Represents an application module that is created by the application.
	/// Implementations should be marked by <see cref="ModuleAttribute"/> to be automatically created by the application.
	/// </summary>
	public interface IModule
	{
		/// <summary>
		/// Initializes the module. This method is called once, after all modules have been initialized.
		/// </summary>
		public void Initialize()
		{
		}

		/// <summary>
		/// Shuts down the module. This method is called once, before the application shuts down.
		/// </summary>
		public void Shutdown()
		{
		}
	}
}