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
				if (wd.IsEnabled && !string.IsNullOrEmpty(wd.Path) && IsSubdirectoryOf(wd.Path, fullPath))
				{
					isAccessed = true;
					break;
				}
			}

			// Order rules by path length (more common rules goes first),
			// then calculate by access mode with overrides.
			foreach (var access in chat.Settings.Environment.DirectoryAccessRules.OrderBy(a => a.Path?.Length ?? 0))
			{
				if (!access.IsEnabled || string.IsNullOrEmpty(access.Path) || !IsSubdirectoryOf(access.Path, fullPath))
					continue;

				if (access.AccessMode == DirectoryAccessMode.None)
				{
					// Deny access but wait for next rules that might allow it.
					isAccessed = false;
					continue;
				}

				isAccessed = (access.AccessMode & mode) != 0;
			}

			return fullPath;
		}

		private static readonly StringComparison _pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

		private static bool IsSubdirectoryOf(string baseDir, string fullPath)
		{
			// Normalize both paths to absolute, canonical form.
			// This resolves relative segments (., ..) and standardizes separators.
			var normalizedBase = Path.GetFullPath(baseDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var normalizedFull = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			// Exact match — accessing the root itself.
			if (string.Equals(normalizedBase, normalizedFull, _pathComparison))
				return true;

			// Check that fullPath starts with baseDir followed by a separator.
			// This prevents partial-name collisions like "C:\Projects" matching "C:\ProjectsSomething".
			return normalizedFull.StartsWith(
				normalizedBase + Path.DirectorySeparatorChar,
				_pathComparison);
		}
	}
}
