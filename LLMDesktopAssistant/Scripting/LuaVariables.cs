using System;
using System.Collections.Generic;
using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting
{
	public static class LuaVariables
	{
		public const CoreModules DefaultModules = CoreModules.Preset_SoftSandbox & ~(CoreModules.Dynamic | CoreModules.Json);

		public const string GlobalTable = "_G";

		public const string NamespaceApiMarker = "_ns_api";
		public const string NamespacePartPath = "_ns_part";
		public const string NamespaceFullPath = "_ns_path";
		public const string NamespaceManuals = "_manuals";

		public const string ToolExecutionContext = "_dass_tool_ctx";
		public const string ToolReactiveResult = "_dass_tool_result";
	}
}