using LLMDesktopAssistant.LLM.Domain;
using System.IO;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services
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

			var extension = Path.GetExtension(url).ToLowerInvariant();

			// максимально тупо, но достаточно для MVP
			var isText = extension is ".txt" or ".log" or ".json" or ".xml" or ".cs" or ".md";

			if (!isText)
				throw new NotSupportedException("Only text files are supported");

			var fileInfo = new FileInfo(url);

			var parameters = new AttachmentApplicationParameters
			{
				SourceUrl = url
			};

			if (fileInfo.Length < 32_000)
			{
				parameters.Mode = AttachmentApplicationMode.FullContents;
			}
			else
			{
				parameters.Mode = AttachmentApplicationMode.PartialContents;
				parameters.StartLineIndex = 0;
				parameters.LineCount = 50;
			}

			return await Task.FromResult(parameters);
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

			string? preview = parameters.Mode switch
			{
				AttachmentApplicationMode.OnlyReference => null,

				AttachmentApplicationMode.FullContents =>
					await ReadLinesAsync(destPath, 0, int.MaxValue, cancellationToken),

				AttachmentApplicationMode.PartialContents =>
					await ReadLinesAsync(destPath,
						parameters.StartLineIndex,
						parameters.LineCount,
						cancellationToken),

				AttachmentApplicationMode.FullHexadecimal =>
					await ReadHexAsync(destPath, 0, int.MaxValue, cancellationToken),

				AttachmentApplicationMode.HexadecimalPartial =>
					await ReadHexAsync(destPath,
						parameters.StartByteIndex,
						parameters.ByteCount,
						cancellationToken),

				_ => null
			};

			var fileInfo = new FileInfo(destPath);

			return new Attachment
			{
				Title = Path.GetFileName(sourcePath),
				SourceUrl = sourcePath,
				LocalPath = localPath,
				Size = (int)fileInfo.Length,
				PreviewContent = preview
			};
		}

		private static async Task<string> ReadLinesAsync(
			string path,
			int start,
			int count,
			CancellationToken ct)
		{
			var lines = new List<string>();

			using var reader = new StreamReader(path);

			int current = 0;

			while (!reader.EndOfStream && lines.Count < count)
			{
				ct.ThrowIfCancellationRequested();

				var line = await reader.ReadLineAsync();
				if (line == null) break;

				if (current >= start)
					lines.Add(line);

				current++;
			}

			string header;
			if (start == 0 && count == int.MaxValue)
				header = "Full attachment:";
			else
				header = $"Lines {start + 1}-{start + count}:";
			return string.Join(Environment.NewLine, lines.Prepend(header));
		}

		private static async Task<string> ReadHexAsync(
			string path,
			int startByte,
			int byteCount,
			CancellationToken ct)
		{
			using var stream = File.OpenRead(path);

			stream.Seek(startByte, SeekOrigin.Begin);

			var buffer = new byte[Math.Min(byteCount, 4096)];
			var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);

			string header;
			if (startByte == 0 && byteCount == int.MaxValue)
				header = "Full bytes:";
			else
				header = $"Bytes {startByte + 1}-{startByte + byteCount}:";

			var sb = new StringBuilder(read * 3 + header.Length + 2);
			sb.AppendLine(header);

			for (int i = 0; i < read; i++)
			{
				sb.Append(buffer[i].ToString("X2"));
				sb.Append(' ');
			}

			return sb.ToString();
		}
	}
}