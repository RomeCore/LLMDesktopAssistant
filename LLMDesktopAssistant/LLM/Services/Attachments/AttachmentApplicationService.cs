using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Utils.Files;
using System.IO;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services.Attachments
{
	public class AttachmentApplicationService(
		Chat chat
		) : IAttachmentApplicationService
	{
		public async Task<AttachmentApplicationParameters> GetRecommendedParamatersAsync(
			string url,
			CancellationToken cancellationToken = default)
		{
			if (!File.Exists(url))
				throw new FileNotFoundException("File not found", url);

			var metrics = FileUtils.GetFileMetrics(url);

			var parameters = new AttachmentApplicationParameters
			{
				SourceUrl = url
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
					parameters.StartByteIndex = 0;
					parameters.ByteCount = 2048;
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
					parameters.StartLineIndex = 1;
					parameters.LineCount = 100;
				}
			}
			else
			{
				parameters.Mode = AttachmentApplicationMode.OnlyReference;
			}

			return parameters;
		}

		public async Task<Attachment> ApplicateAttachmentAsync(
			AttachmentApplicationParameters parameters,
			CancellationToken cancellationToken = default)
		{
			var sourcePath = parameters.SourceUrl;

			if (!File.Exists(sourcePath))
				throw new FileNotFoundException("File not found", sourcePath);

			var workingDir = chat.Settings.GetWorkingDirectory();
			var attachmentsDir = Path.Combine(workingDir, ".llmassist", "attachments");
			Directory.CreateDirectory(attachmentsDir);

			var fileName = $"{DateTime.Now:yyyy-MM-dd-HHmmss}-{Path.GetFileName(sourcePath)}";
			var localPath = Path.Combine(".llmassist", "attachments", fileName);
			var destPath = Path.Combine(attachmentsDir, fileName);

			File.Copy(sourcePath, destPath, overwrite: true);

			var metrics = FileUtils.GetFileMetrics(destPath);

			string? preview = parameters.Mode switch
			{
				AttachmentApplicationMode.OnlyReference => BuildReferencePreview(localPath, metrics),

				AttachmentApplicationMode.FullContents =>
					BuildTextPreview(destPath, 1, int.MaxValue, metrics),

				AttachmentApplicationMode.PartialContents =>
					BuildTextPreview(destPath,
						parameters.StartLineIndex,
						parameters.LineCount,
						metrics),

				AttachmentApplicationMode.FullHexadecimal =>
					BuildHexPreview(destPath, 0, int.MaxValue, metrics),

				AttachmentApplicationMode.HexadecimalPartial =>
					BuildHexPreview(destPath,
						parameters.StartByteIndex,
						parameters.ByteCount,
						metrics),

				_ => null
			};

			return new Attachment
			{
				Title = Path.GetFileName(sourcePath),
				SourceUrl = sourcePath,
				LocalPath = localPath,
				Size = (int)metrics.Size,
				PreviewContent = preview
			};
		}

		private static string BuildTextPreview(
			string path,
			int start,
			int count,
			FileMetrics metrics)
		{
			var (lines, total) = FileUtils.ReadLinesChunk(
				path,
				start,
				count,
				20000,
				true);

			return $"""
				[TEXT FILE]
				Total lines: {total}
				Showing lines {start}-{start + lines.Count - 1}

				NOTE:
				The file content is already provided below.
				DO NOT call fs-read_file unless more context is required.

				----- BEGIN CONTENT -----
				{string.Join(Environment.NewLine, lines)}
				----- END CONTENT -----
				""";
		}

		private static string BuildHexPreview(
			string path,
			int start,
			int count,
			FileMetrics metrics)
		{
			var (lines, read) = FileUtils.ReadHexChunk(path, start, count);

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

				NOTE:
				The file is not included due to size.
				Use tools to inspect it if needed.
				""";
		}
	}
}