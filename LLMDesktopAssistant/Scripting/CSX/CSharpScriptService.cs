using System.Reflection;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace LLMDesktopAssistant.Scripting.CSX
{
	/// <summary>
	/// Executes C# scripts (.csx) with the provided globals.
	/// Scripts are compiled with Roslyn on every execution and have full access to all loaded assemblies.
	/// </summary>
	[Service]
	public class CSharpScriptService
	{
		private static readonly ScriptOptions Options = ScriptOptions.Default
			.AddReferences(GetLoadedAssemblies())
			.WithImports(
				"System",
				"System.IO",
				"System.Collections.Generic",
				"System.Linq",
				"System.Net.Http",
				"System.Text",
				"System.Text.Json",
				"System.Text.Json.Nodes",
				"System.Threading",
				"System.Threading.Tasks",
				"LLMDesktopAssistant",
				"LLMDesktopAssistant.Tools",
				"LLMDesktopAssistant.Tools.Meta",
				"LLMDesktopAssistant.Scripting",
				"LLMDesktopAssistant.Utils");

		/// <summary>
		/// Compiles and runs a C# script with the provided globals.
		/// </summary>
		/// <param name="code">The C# script source code.</param>
		/// <param name="globals">The globals object passed to the script.</param>
		/// <param name="cancellationToken">The token to cancel the script execution.</param>
		/// <returns>The return value of the script, or <see langword="null"/> when the script returns nothing.</returns>
		public async Task<object?> RunAsync(string code, object? globals, CancellationToken cancellationToken = default)
		{
			var globalsType = globals?.GetType();
			var script = CSharpScript.Create<object?>(code, Options, globalsType);
			var state = await script.RunAsync(globals: globals, cancellationToken: cancellationToken);
			return state.ReturnValue;
		}

		private static IReadOnlyList<Assembly> GetLoadedAssemblies()
		{
			try
			{
				return ReflectionUtility.AllAssemblies
					.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
					.ToArray();
			}
			catch (InvalidOperationException)
			{
				return AppDomain.CurrentDomain.GetAssemblies()
					.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
					.ToArray();
			}
		}
	}
}
