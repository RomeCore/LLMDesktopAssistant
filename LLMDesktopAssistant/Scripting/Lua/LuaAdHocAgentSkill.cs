using AsyncLua;
using AsyncLua.Values;
using LLMDesktopAssistant.Agents.Tasks;

namespace LLMDesktopAssistant.Scripting.Lua
{
	public class LuaAdHocAgentSkill : AgentSkill
	{
		public override string Name { get; }
		public override string Description { get; }
		public override string? Path { get; }
		public override string? HomeDirectory { get; }

		/// <summary>
		/// The calling context for the Lua script to call <see cref="Function"/>.
		/// </summary>
		public LuaCallingContext? Context { get; }

		/// <summary>
		/// The Lua function to be called when body is being requested.
		/// </summary>
		public LuaFunction? Function { get; }

		/// <summary>
		/// The body of the skill, which will be returned when <see cref="Function"/> is null.
		/// </summary>
		public string? Body { get; }

		public LuaAdHocAgentSkill(string name, string description, string? path, string? homeDirectory, LuaCallingContext context, LuaFunction function)
		{
			Name = name;
			Description = description;
			Path = path;
			HomeDirectory = homeDirectory;
			Context = context;
			Function = function;
		}

		public LuaAdHocAgentSkill(string name, string description, string? path, string? homeDirectory, string body)
		{
			Name = name;
			Description = description;
			Path = path;
			HomeDirectory = homeDirectory;
			Body = body;
		}

		public override async Task<string> GetBodyAsync(CancellationToken cancellationToken = default)
		{
			if (Function != null)
				return (await Function.InvokeAsync(Context!)).ToString();
			return Body!;
		}
	}
}