namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Provides a way to mark classes as being part of the Lua API.
	/// Class marked with this attribute is registered in the chat services using DI.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class LuaApiAttribute : Attribute
	{
	}
}
