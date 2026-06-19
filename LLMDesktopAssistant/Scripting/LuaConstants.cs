using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting
{
	public static class LuaConstants
	{
		/// <summary>
		/// Default modules for the Lua interpreter.
		/// </summary>
		public const CoreModules DefaultModules = CoreModules.Preset_SoftSandbox & ~(CoreModules.Dynamic | CoreModules.Json);

		/// <summary>
		/// List of types that metatables can be bound to in Lua.
		/// Includes nil, void, boolean, number, string, and function.
		/// </summary>
		public static readonly ImmutableArray<DataType> SupportedTypeMetatables = [
			DataType.Nil,
			DataType.Void,
			DataType.Boolean,
			DataType.Number,
			DataType.String,
			DataType.Function
		];

	}
}