using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Services.Instances;
using LLTSharp;
using LLTSharp.DataAccessors;

namespace LLMDesktopAssistant.Prompting.Plugins
{
	[ChatService(typeof(IPromptTemplatePlugin))]
	public class FilesystemPromptTemplatePlugin(
		WorkingDirectoryAccessService fileAccess
		) : IPromptTemplatePlugin
	{
		public IEnumerable<TemplateFunction> GetTemplateFunctions()
		{
			yield return new TemplateFunction("fileExists", FileExists);
			yield return new TemplateFunction("directoryExists", DirectoryExists);
			yield return new TemplateFunction("readFile", ReadFile);
			yield return new TemplateFunction("listFiles", ListFiles);
			yield return new TemplateFunction("listDirectories", ListDirectories);
		}

		public TemplateDataAccessor FileExists(TemplateDataAccessor? self, TemplateDataAccessor[] args)
		{
			if (args.Length < 1)
				throw new TemplateRuntimeException("fileExists(path): Invalid arguments. Expected at least one argument: file path.");

			var path = args[0].ToString();
			var fullPath = fileAccess.TryAccessPath(path);

			if (fullPath == null)
				return new TemplateBooleanAccessor(false);

			try
			{
				return new TemplateBooleanAccessor(File.Exists(fullPath));
			}
			catch
			{
				return new TemplateBooleanAccessor(false);
			}
		}

		public TemplateDataAccessor DirectoryExists(TemplateDataAccessor? self, TemplateDataAccessor[] args)
		{
			if (args.Length < 1)
				throw new TemplateRuntimeException("directoryExists(path): Invalid arguments. Expected at least one argument: directory path.");

			var path = args[0].ToString();
			var fullPath = fileAccess.TryAccessPath(path);

			if (fullPath == null)
				return new TemplateBooleanAccessor(false);

			try
			{
				return new TemplateBooleanAccessor(Directory.Exists(fullPath));
			}
			catch
			{
				return new TemplateBooleanAccessor(false);
			}
		}

		public TemplateDataAccessor ReadFile(TemplateDataAccessor? self, TemplateDataAccessor[] args)
		{
			if (args.Length < 1)
				throw new TemplateRuntimeException("readFile(path): Invalid arguments. Expected at least one argument: file path.");
			
			var path = args[0].ToString();
			var fullPath = fileAccess.TryAccessPath(path);

			if (fullPath == null)
				throw new TemplateRuntimeException($"readFile({path}): File cannot be accessed.");

			if (!File.Exists(fullPath))
				throw new TemplateRuntimeException($"readFile({path}): File does not exist.");

			try
			{
				return new TemplateStringAccessor(File.ReadAllText(fullPath));
			}
			catch (Exception ex)
			{
				throw new TemplateRuntimeException($"readFile({path}): Error reading file. {ex.Message}");
			}
		}

		public TemplateDataAccessor ListFiles(TemplateDataAccessor? self, TemplateDataAccessor[] args)
		{
			if (args.Length < 1)
				throw new TemplateRuntimeException("listFiles(path): Invalid arguments. Expected at least one argument: directory path.");

			var path = args[0].ToString();
			var fullPath = fileAccess.TryAccessPath(path);

			if (fullPath == null)
				throw new TemplateRuntimeException($"listFiles({path}): Directory cannot be accessed.");

			if (!Directory.Exists(fullPath))
				throw new TemplateRuntimeException($"listFiles({path}): Directory does not exist.");

			try
			{
				var files = Directory.GetFiles(fullPath);
				return new TemplateArrayAccessor(files.Select(f => new TemplateStringAccessor(f)));
			}
			catch (Exception ex)
			{
				throw new TemplateRuntimeException($"listFiles({path}): Error listing files. {ex.Message}");
			}
		}

		public TemplateDataAccessor ListDirectories(TemplateDataAccessor? self, TemplateDataAccessor[] args)
		{
			if (args.Length < 1)
				throw new TemplateRuntimeException("listDirectories(path): Invalid arguments. Expected at least one argument: directory path.");

			var path = args[0].ToString();
			var fullPath = fileAccess.TryAccessPath(path);

			if (fullPath == null)
				throw new TemplateRuntimeException($"listDirectories({path}): Directory cannot be accessed.");

			if (!Directory.Exists(fullPath))
				throw new TemplateRuntimeException($"listDirectories({path}): Directory does not exist.");

			try
			{
				var directories = Directory.GetDirectories(fullPath);
				return new TemplateArrayAccessor(directories.Select(f => new TemplateStringAccessor(f)));
			}
			catch (Exception ex)
			{
				throw new TemplateRuntimeException($"listDirectories({path}): Error listing directories. {ex.Message}");
			}
		}
	}
}