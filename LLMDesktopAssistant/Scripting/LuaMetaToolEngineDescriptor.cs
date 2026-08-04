using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools.Meta;

namespace LLMDesktopAssistant.Scripting
{
	public class LuaMetaToolEngineDescriptor : IMetaToolEngineDescriptor
	{
		public ScriptLanguageType Language => ScriptLanguageType.Lua;

		public string MainExtension => ".lua";

		public string[] Extensions => [ ".lua", ".alua" ];

		public string FrontmatterStart => "--[[";

		public string FrontmatterEnd => "]]";

		public string Examples => """
			Example arguments:
			{"location": "New York", "days": 3}

			Example code:
			-- Fetch weather data
			local url = "https://api.weather.com/forecast?q=" .. tool_args.location .. "&days=" .. tool_args.days
			local result = await web.fetch(url)
			print("Weather in " .. tool_args.location .. ": " .. result)
			""";
	}
}