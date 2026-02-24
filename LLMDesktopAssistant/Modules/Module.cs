namespace LLMDesktopAssistant.Modules
{
	/// <summary>
	/// Represents an abstract base class for application modules.
	/// </summary>
	public abstract class Module : IModule
	{
		public virtual void Initialize()
		{
		}

		public virtual void Shutdown()
		{
		}
	}
}