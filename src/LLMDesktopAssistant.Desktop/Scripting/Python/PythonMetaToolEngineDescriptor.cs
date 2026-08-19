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
			        print(f"Current temperature: {weather.temperature}�C")
			
			asyncio.run(getweather())
			""";

		public string Template => """"
			"""
			title: My Tool
			description: Describe what this tool does and when to use it.
			category: general
			approval_level: policy-based
			behaviours:
			  - file_read
			argument_schema: |
			  {
			    "type": "object",
			    "properties": {
			      "input": {
			        "type": "string",
			        "description": "The input to process"
			      }
			    },
			    "additionalProperties": false
			  }
			"""

			# Arguments come as a dict: tool_args["input"]
			input_value = tool_args.get("input")

			# Print the result to return it to the LLM
			print("Processed: " + str(input_value))
			"""";
	}
}
