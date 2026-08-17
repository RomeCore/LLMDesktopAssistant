using System.Collections.Concurrent;

namespace LLMDesktopAssistant.Tools.Consents
{
	public class ToolConsentTaskScope
	{
		private static readonly AsyncLocal<ToolConsentTaskScope?> _currentScope = new();

		/// <summary>
		/// Gets or sets the current scope. This is used to track the consent status of tools within the current execution context.
		/// </summary>
		public static ToolConsentTaskScope? Current
		{
			get => _currentScope.Value;
			set => _currentScope.Value = value;
		}

		/// <summary>
		/// A dictionary to store memorized decisions for tools.
		/// </summary>
		internal readonly ConcurrentDictionary<string, MemorizedDecision> memorized = [];
	}
}
