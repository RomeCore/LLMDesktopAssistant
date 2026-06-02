using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Utils.Files;
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
			uri.Host.StartsWith("http", StringComparison.OrdinalIgnoreCase);

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

		public async Task<AttachmentApplicationParameters> GetRecommendedParamatersAsync(
			Uri uri,
			CancellationToken cancellationToken = default)
		{
			var isWebUrl = IsWebUrl(uri);
			string? tempPath = null;

			try
			{
				string pathToAnalyze;

				if (isWebUrl)
				{
					tempPath = Path.Combine(
						Path.GetTempPath(),
						$"llmattach-{Guid.NewGuid():N}");

					pathToAnalyze = await EnsureLocalFileAsync(uri, tempPath, cancellationToken);
				}
				else
				{
					var fileName = uri.LocalPath;

					if (!File.Exists(fileName))
						throw new FileNotFoundException("File not found", fileName);

					pathToAnalyze = fileName;
				}

				var metrics = FileUtils.GetFileMetrics(pathToAnalyze);

				var parameters = new AttachmentApplicationParameters
				{
					SourceUri = uri
				};

				if (metrics.IsBinary)
				{
					if (metrics.Size < 8_000)
					{
						parameters.Mode = AttachmentApplicationMode.FullHexadecimal;
					}
					else
					{
						parameters.Mode = AttachmentApplicationMode.HexadecimalPartial;
						parameters.StartByte = 1;
						parameters.EndByte = 2048;
					}
				}
				else if (metrics.LineCount is int lines)
				{
					if (lines < 200)
					{
						parameters.Mode = AttachmentApplicationMode.FullContents;
					}
					else
					{
						parameters.Mode = AttachmentApplicationMode.PartialContents;
						parameters.StartLine = 1;
						parameters.EndLine = 100;
					}
				}
				else
				{
					parameters.Mode = AttachmentApplicationMode.OnlyReference;
				}

				return parameters;
			}
			finally
			{
				if (tempPath != null)
				{
					try { File.Delete(tempPath); } catch { }
				}
			}
		}

		public async Task<Attachment> ApplicateAttachmentAsync(
			AttachmentApplicationParameters parameters,
			CancellationToken cancellationToken = default)
		{
			var sourceUri = parameters.SourceUri;

			var workingDir = chat.Settings.Environment.GetWorkingDirectory();
			var attachmentsDir = Path.Combine(workingDir, ".llmassist", "attachments");
			Directory.CreateDirectory(attachmentsDir);

			var destPath = GetDestinationPath(sourceUri, attachmentsDir);
			var localPath = Path.GetRelativePath(workingDir, destPath);

			await EnsureLocalFileAsync(sourceUri, destPath, cancellationToken);

			var metrics = FileUtils.GetFileMetrics(destPath);

			string? preview = parameters.Mode switch
			{
				AttachmentApplicationMode.OnlyReference =>
					BuildReferencePreview(localPath, metrics),

				AttachmentApplicationMode.FullContents =>
					BuildTextPreview(destPath, 1, int.MaxValue, metrics),

				AttachmentApplicationMode.PartialContents =>
					BuildTextPreview(destPath,
						parameters.StartLine,
						parameters.EndLine,
						metrics),

				AttachmentApplicationMode.FullHexadecimal =>
					BuildHexPreview(destPath, 1, int.MaxValue, metrics),

				AttachmentApplicationMode.HexadecimalPartial =>
					BuildHexPreview(destPath,
						parameters.StartByte,
						parameters.EndByte,
						metrics),

				_ => null
			};

			return new Attachment
			{
				Title = Path.GetFileName(sourceUri.LocalPath),
				SourceUrl = sourceUri.AbsoluteUri,
				LocalPath = localPath,
				Size = (int)metrics.Size,
				PreviewContent = preview
			};
		}

		private static string BuildTextPreview(
			string path,
			int start,
			int end,
			FileMetrics metrics)
		{
			var (lines, total) = FileUtils.ReadLinesChunk(
				path,
				start,
				end - start + 1,
				20000,
				true);

			return $"""
				The file content is already provided below.
				DO NOT call fs-read_entry unless more context is required.
				----- BEGIN CONTENT -----
				{string.Join(Environment.NewLine, lines)}
				-----  END CONTENT  -----
				""";
		}

		private static string BuildHexPreview(
			string path,
			int start,
			int end,
			FileMetrics metrics)
		{
			var (lines, read) = FileUtils.ReadHexChunk(
				path,
				start,
				end - start + 1,
				bytesPerLine: 16);

			return $"""
				[BINARY FILE]
				Showing bytes {start}-{start + read - 1}
				----- HEX DUMP -----
				{string.Join(Environment.NewLine, lines)}
				--------------------
				""";
		}

		private static string BuildReferencePreview(
			string localPath,
			FileMetrics metrics)
		{
			return $"""
				[REFERENCE]
				The file is not included due to size.
				Use tools to inspect it if needed.
				""";
		}
	}
}