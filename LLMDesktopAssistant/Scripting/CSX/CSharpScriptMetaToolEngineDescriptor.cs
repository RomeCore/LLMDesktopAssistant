using LLMDesktopAssistant.Tools.Meta;

namespace LLMDesktopAssistant.Scripting.CSX
{
	/// <summary>
	/// Descriptor for C# Script (.csx) meta tools with YAML frontmatter in <c>/* ... */</c> comment blocks.
	/// </summary>
	public class CSharpScriptMetaToolEngineDescriptor : IMetaToolEngineDescriptor
	{
		/// <inheritdoc/>
		public ScriptLanguageType Language => ScriptLanguageType.CSharpScript;

		/// <inheritdoc/>
		public string MainExtension => ".csx";

		/// <inheritdoc/>
		public string[] Extensions => [ ".csx" ];

		/// <inheritdoc/>
		public string FrontmatterStart => "/*";

		/// <inheritdoc/>
		public string FrontmatterEnd => "*/";

		/// <inheritdoc/>
		public string Examples => """
			Example arguments:
			{"location": "New York", "days": 3}

			Example code:
			// Arguments come as JsonNode, guard optional ones
			var location = (string?)ToolArgs?["location"];
			var days = (int?)ToolArgs?["days"] ?? 10;

			// Optional: streaming UI feedback for long-running tools
			Result.SetStatus("Search", "Searching...");
			Result.UseMarkdown = true;
			Result.Write("Step 1: doing something...");

			// Full access to .NET APIs and dASS assemblies
			using var http = new HttpClient();
			var json = await http.GetStringAsync($"https://api.weather.com/forecast?q={location}&days={days}");

			// Return the result to the LLM (structured + text)
			Result.SetStructured(new { location, days });
			Result.Write("Weather: " + json);
			Result.SetStatus("Check", "Done!");
			Result.CompleteWithSuccess();
			return json;
			""";
	}
}
