using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Utils.Files;
using LLMDesktopAssistant.Utils;
using System.IO;
using System.Net.Http;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services.Attachments
{
	[ChatService(typeof(IAttachmentApplicationService))]
	public class AttachmentApplicationService(
		Chat chat
		) : IAttachmentApplicationService
	{
		private static readonly HttpClient HttpClient = new()
		{
			Timeout = TimeSpan.FromMinutes(5)
		};

		private static bool IsWebUrl(Uri uri) =>
			uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase);

		private static async Task<string> EnsureLocalFileAsync(
			Uri uri,
			string destPath,
			CancellationToken cancellationToken = default)
		{
			if (IsWebUrl(uri))
			{
				using var response = await HttpClient.GetAsync(
					uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
				response.EnsureSuccessStatusCode();

				Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

				await using var fs = new FileStream(
					destPath, FileMode.Create, FileAccess.Write, FileShare.None,
					bufferSize: 8192, useAsync: true);
				await response.Content.CopyToAsync(fs, cancellationToken);

				return destPath;
			}

			var fileName = uri.LocalPath;

			if (!File.Exists(fileName))
				throw new FileNotFoundException("File not found", fileName);

			Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
			File.Copy(fileName, destPath, overwrite: true);

			return destPath;
		}

		private static string GetDestinationPath(Uri uri, string attachmentsDir)
		{
			var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");

			string fileName;

			if (IsWebUrl(uri))
			{
				var nameFromUrl = Path.GetFileName(uri.LocalPath);

				if (!string.IsNullOrWhiteSpace(nameFromUrl) && nameFromUrl.Contains('.'))
				{
					fileName = $"{timestamp}-{SanitizeFileName(nameFromUrl)}";
				}
				else
				{
					var host = SanitizeFileName(uri.Host);
					fileName = $"{timestamp}-{host}.html";
				}
			}
			else
			{
				fileName = $"{timestamp}-{Path.GetFileName(uri.LocalPath)}";
			}

			return Path.Combine(attachmentsDir, fileName);
		}

		private static string SanitizeFileName(string name)
		{
			var invalid = Path.GetInvalidFileNameChars();
			foreach (var c in invalid)
				name = name.Replace(c, '_');
			return name;
		}

		public async Task<Attachment> ApplyAttachmentAsync(
			AttachmentApplicationParameters parameters,
			CancellationToken cancellationToken = default)
		{
			var sourceUri = parameters.SourceUri;

			var workingDir = chat.Settings.Environment.GetWorkingDirectory();
			var attachmentsDir = Path.Combine(workingDir, Directories.WorkingHome, "attachments");
			Directory.CreateDirectory(attachmentsDir);

			var destPath = GetDestinationPath(sourceUri, attachmentsDir);
			var localPath = Path.GetRelativePath(workingDir, destPath);

			await EnsureLocalFileAsync(sourceUri, destPath, cancellationToken);

			var metrics = FileUtils.GetFileMetrics(destPath);

			return new Attachment
			{
				Title = Path.GetFileName(sourceUri.LocalPath),
				SourceUrl = sourceUri.AbsoluteUri,
				LocalPath = localPath,
				Size = (int)metrics.Size,
				Lines = metrics.LineCount
			};
		}
	}
}