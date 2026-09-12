using LLMDesktopAssistant.Scripting.Lua;
using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class LuaScriptsetSettings : NotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value indicating whether the unchanged Lua scripts should be enabled by default.
		/// </summary>
		public bool EnabledByDefault
		{
			get;
			set => SetProperty(ref field, value);
		} = true;

		/// <summary>
		/// Gets or sets the Lua script changes.
		/// </summary>
		public RangeObservableCollection<LuaScriptChange> ScriptChanges
		{
			get => field ??= [];
			set => (field ??= []).Reset(value);
		}
	}
}
