namespace LLMDesktopAssistant.Scripting.Lua
{
	public interface ILuaScriptsetStateWatcher
	{
		event Action? OnStateChanged;

		IEnumerable<LuaScriptInfo> GetEffectiveScripts();
	}
}