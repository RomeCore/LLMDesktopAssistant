namespace LLMDesktopAssistant.Desktop.Execution
{
	public static class WSLPathConverter
	{
		public static string ConvertToWslPath(string windowsPath)
		{
			if (string.IsNullOrWhiteSpace(windowsPath))
				return windowsPath;
			windowsPath = windowsPath.Replace('\\', '/');

			// Handle drive letter (C:/path -> /mnt/host/c/path)
			if (windowsPath.Length >= 2 && windowsPath[1] == ':')
			{
				char drive = char.ToLower(windowsPath[0]);
				string pathAfterDrive = windowsPath.Substring(2);
				return $"/mnt/host/{drive}{pathAfterDrive}";
			}

			return windowsPath;
		}
	}
}