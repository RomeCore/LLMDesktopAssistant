using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	/// <summary>
	/// The attribute to mark a class as a scoped chat service.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ChatServiceAttribute(Type? serviceType = null) : Attribute
	{
		/// <summary>
		/// Gets the type of the chat service.
		/// </summary>
		public Type? ServiceType { get; } = serviceType;
	}
}