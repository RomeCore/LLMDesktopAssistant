using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Services.Instances
{
	/// <summary>
	/// The service for accessing files inside current working directory.
	/// </summary>
	/// <param name="chat">The current chat instance that contains environment settings.</param>
	[ChatService]
	public class WorkingDirectoryAccessService(Chat chat)
	{
		public string GetWorkingDirectory()
		{
			return chat.Settings.Environment.GetWorkingDirectory();
		}

		public string AccessPath(string path)
		{
			return TryAccessPath(path) ??
				throw new AccessViolationException("Access outside working directory is not allowed.");
		}

		public string? TryAccessPath(string path)
		{
			var baseDir = Path.GetFullPath(chat.Settings.Environment.GetWorkingDirectory());
			if (string.IsNullOrWhiteSpace(path) || path == ".")
				return baseDir;

			var fullPath = Path.GetFullPath(Path.Combine(baseDir, path));

			if (!fullPath.StartsWith(baseDir))
				return null;

			return fullPath;
		}

		public string ForceAccessPath(string path, out bool isAccessed)
		{
			var baseDir = Path.GetFullPath(chat.Settings.Environment.GetWorkingDirectory());
			if (string.IsNullOrWhiteSpace(path) || path == ".")
			{
				isAccessed = true;
				return baseDir;
			}

			var fullPath = Path.GetFullPath(Path.Combine(baseDir, path));
			isAccessed = fullPath.StartsWith(baseDir);
			return fullPath;
		}
	}
}
