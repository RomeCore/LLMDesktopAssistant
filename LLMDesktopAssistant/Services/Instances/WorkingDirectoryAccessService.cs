using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
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

		public string? TryAccessPath(string path, DirectoryAccessMode mode)
		{
			var fullPath = CheckedAccessPath(path, mode, out bool isAccessed);
			if (!isAccessed)
				return null;
			return fullPath;
		}

		public string AccessPath(string path, DirectoryAccessMode mode)
		{
			var fullPath = CheckedAccessPath(path, mode, out bool isAccessed);
			if (!isAccessed)
				throw new UnauthorizedAccessException("The path cannot be accessed because of access restrictions.");
			return fullPath;
		}

		public string CheckedAccessPath(string path, DirectoryAccessMode mode, out bool isAccessed)
		{
			var baseDir = Path.GetFullPath(chat.Settings.Environment.GetWorkingDirectory());
			var fullPath = string.IsNullOrEmpty(path) ? baseDir : Path.GetFullPath(Path.Combine(baseDir, path));
			
			isAccessed = false;

			// Any of working directories are allowed to access.
			foreach (var wd in chat.Settings.Environment.WorkingDirectories)
			{
				if (wd.IsEnabled && !string.IsNullOrEmpty(wd.Path) && fullPath.StartsWith(wd.Path))
				{
					isAccessed = true;
					break;
				}
			}

			// Order rules by path length (more common rules goes first),
			// then calculate by access mode with overrides.
			foreach (var access in chat.Settings.Environment.DirectoryAccessRules.OrderBy(a => a.Path?.Length ?? 0))
			{
				if (string.IsNullOrEmpty(access.Path) || !access.IsEnabled || !fullPath.StartsWith(access.Path))
					continue;

				if (access.AccessMode == DirectoryAccessMode.None)
				{
					// Deny access but wait for next rules that might allow it.
					isAccessed = false;
					continue;
				}

				isAccessed = isAccessed || (access.AccessMode & mode) != 0;
			}

			return fullPath;
		}
	}
}
