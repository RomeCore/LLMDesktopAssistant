using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools.Meta;

namespace LLMDesktopAssistant.Desktop.Scripting.Python
{
	public class PythonMetaToolEngineDescriptor : IMetaToolEngineDescriptor
	{
		public ScriptLanguageType Language => ScriptLanguageType.Python;

		public string MainExtension => ".py";

		public string[] Extensions => [".py"];

		public string FrontmatterStart => @"""""""";

		public string FrontmatterEnd => @"""""""";

		public string Examples => """
			Example arguments:
			{"location": "New York", "days": 3}
			
			Example code:
			import python_weather
			import asyncio
			
			async def getweather():
			    async with python_weather.Client() as client:
			        location = tool_args["location"]
			        weather = await client.get(location)
			        print(f"Current temperature: {weather.temperature}°C")
			
			asyncio.run(getweather())
			""";
	}
}
