using System;
using System.Collections.Generic;
using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[LuaApi]
	public class LuaApiRoot : LuaApiBase
	{
		public override string? Namespace => "dass";

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			
		}
	}
}
