using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The attribute to mark a class as a tool module.
	/// Classes marked by this attribute will be created 
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ToolModuleAttribute(bool chatScoped = true) : Attribute
	{
		/// <summary>
		/// Indicates whether the tool module is scoped to a chat services.
		/// </summary>
		public bool ChatScoped { get; } = chatScoped;
	}
}