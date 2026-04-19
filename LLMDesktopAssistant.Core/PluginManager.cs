using LLMDesktopAssistant.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant
{
	/// <summary>
	/// Manages the loading of plugins into the application.
	/// </summary>
	public static class PluginManager
	{
		/// <summary>
		/// Loads all plugins into the specified application domain.
		/// </summary>
		/// <param name="domain">The application domain to load plugins into.</param>
		public static void LoadPluginsInto(AppDomain domain)
		{
			string[] searchPaths = [
				Environment.CurrentDirectory,
				Directories.Plugins,
				Path.Combine(Directories.Plugins, "dependencies")
			];
			Directory.CreateDirectory(Path.Combine(Directories.Plugins, "dependencies"));

			Dictionary<string, Assembly> loadedAssemblies = [];

			Assembly? OnResolve(object? s, ResolveEventArgs e)
			{
				if (loadedAssemblies.TryGetValue(e.Name, out var assembly))
					return assembly;

				foreach (var path in searchPaths)
				{
					foreach (var asmFile in Directory.GetFiles(path, "*.dll"))
					{
						var assemblyInfo = AssemblyName.GetAssemblyName(asmFile);
						if (assemblyInfo.FullName == e.Name)
						{
							var asm = domain.Load(asmFile);
							loadedAssemblies.Add(e.Name, asm);
							return asm;
						}
					}
				}

				return null;
			}

			domain.AssemblyResolve += OnResolve;

			try
			{
				foreach (var plugin in Directory.GetFiles(Directories.Plugins, "*.dll"))
				{
					var assemblyInfo = AssemblyName.GetAssemblyName(plugin);
					if (loadedAssemblies.ContainsKey(assemblyInfo.FullName))
						continue;

					domain.Load(assemblyInfo);
				}
			}
			finally
			{
				domain.AssemblyResolve -= OnResolve;
			}
		}
	}
}