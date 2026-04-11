using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.Utils.Files
{
	public static class FileUtils
	{
		private static readonly HashSet<string> TextExtensions = new()
		{
			".txt",".md",".csv",".json",".xml",".yaml",".yml",".toml",".ini",".cfg",".conf",".env",".log",
			".sh",".bat",".cmd",".ps1",".bash",".zsh"
		};

		private static readonly HashSet<string> CodeExtensions = new()
		{
			".cs",".py",".js",".ts",".jsx",".tsx",".java",".cpp",".c",".h",".hpp",".go",".rs",".rb",".php",
			".swift",".kt",".scala",".lua",".r",".m",".sql",".html",".css",".scss",".less",".vue",".svelte"
		};

		private static readonly HashSet<string> DocumentExtensions = new()
		{
			".doc",".docx",".pdf",".xls",".xlsx",".ppt",".pptx",".odt",".ods",".odp",".rtf"
		};

		private static readonly HashSet<string> ImageExtensions = new()
		{
			".png",".jpg",".jpeg",".gif",".bmp",".ico",".svg",".webp",".tiff",".tif"
		};

		private static readonly HashSet<string> AudioExtensions = new()
		{
			".mp3",".wav",".ogg",".flac",".aac",".wma",".m4a"
		};

		private static readonly HashSet<string> VideoExtensions = new()
		{
			".mp4",".avi",".mkv",".mov",".wmv",".flv",".webm",".m4v"
		};

		private static readonly HashSet<string> ArchiveExtensions = new()
		{
			".zip",".rar",".7z",".tar",".gz",".bz2",".xz"
		};

		private static readonly HashSet<string> ExecutableExtensions = new()
		{
			".exe",".dll",".so",".dylib",".msi",".app",".deb",".rpm"
		};

		public static FileType GetFileType(string path)
		{
			var ext = Path.GetExtension(path).ToLowerInvariant();

			if (CodeExtensions.Contains(ext)) return FileType.Code;
			if (TextExtensions.Contains(ext)) return FileType.Text;
			if (DocumentExtensions.Contains(ext)) return FileType.Document;
			if (ImageExtensions.Contains(ext)) return FileType.Image;
			if (AudioExtensions.Contains(ext)) return FileType.Audio;
			if (VideoExtensions.Contains(ext)) return FileType.Video;
			if (ArchiveExtensions.Contains(ext)) return FileType.Archive;
			if (ExecutableExtensions.Contains(ext)) return FileType.Executable;
			if (IsBinaryFile(path)) return FileType.Binary;

			return FileType.Text;
		}

		/// <summary>
		/// Determines if a file is binary or not. A file is considered binary if it contains non-text characters.
		/// </summary>
		/// <param name="fullPath">The full path of the file to check.</param>
		/// <returns>True if the file is binary, false otherwise.</returns>
		public static bool IsBinaryFile(string fullPath)
		{
			const int sampleSize = 8000;
			var buffer = new byte[sampleSize];

			using var fs = File.OpenRead(fullPath);
			var bytesRead = fs.Read(buffer, 0, buffer.Length);

			for (var i = 0; i < bytesRead; i++)
			{
				var b = buffer[i];
				if (b == 0)
					return true;
				if (b < 32 && b is not 9 and not 10 and not 13)
					return true;
			}

			return false;
		}

		public static int CountLines(string filePath)
		{
			var lines = File.ReadLines(filePath).Count();
			return lines;
		}

		public static string BytesToDisplaySize(long bytes)
		{
			var sizeStr = $"{bytes} B";

			if (bytes > 10240)
			{
				bytes /= 1024;
				sizeStr = $"{bytes} KB";

				if (bytes > 10240)
				{
					bytes /= 1024;
					sizeStr = $"{bytes} MB";

					if (bytes > 10240)
					{
						bytes /= 1024;
						sizeStr = $"{bytes} GB";
					}
				}
			}

			return sizeStr;
		}

		public static FileMetrics GetFileMetrics(string fullPath)
		{
			if (!File.Exists(fullPath))
				throw new FileNotFoundException("File not found", fullPath);

			var info = new FileInfo(fullPath);

			var isBinary = IsBinaryFile(fullPath);
			var type = GetFileType(fullPath);

			int? lines = null;

			if (!isBinary)
			{
				try
				{
					lines = CountLines(fullPath);
				}
				catch
				{
				}
			}

			return new FileMetrics
			{
				Name = info.Name,
				FullPath = fullPath,
				Size = info.Length,
				Type = type,
				IsBinary = isBinary,
				LineCount = lines,
				Created = info.CreationTime,
				Modified = info.LastWriteTime,
				Attributes = info.Attributes
			};
		}

		public static (List<string> Lines, int TotalLines) ReadLinesChunk(
			string fullPath,
			int lineStart,
			int lineCount,
			int maxLineLength,
			bool withLineNumbers)
		{
			var result = new List<string>();

			int currentLine = 1;
			int collected = 0;

			using var reader = new StreamReader(fullPath);

			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine();
				if (line == null) break;

				if (currentLine >= lineStart && collected < lineCount)
				{
					if (line.Length > maxLineLength)
						line = line[..maxLineLength] + "... (truncated)";

					if (withLineNumbers)
						line = $"{currentLine,6}: {line}";

					result.Add(line);
					collected++;
				}

				currentLine++;
			}

			return (result, currentLine - 1);
		}

		public static (List<string> Lines, int BytesRead) ReadHexChunk(
			string fullPath,
			int startByte,
			int bytesCount,
			int bytesPerLine = 16)
		{
			using var fs = File.OpenRead(fullPath);

			if (startByte < 1)
				throw new ArgumentOutOfRangeException(nameof(startByte), "Start byte must be greater than zero.");
			if (startByte >= fs.Length)
				return (new List<string>(), 0);

			startByte--;
			fs.Seek(startByte, SeekOrigin.Begin);

			var buffer = new byte[Math.Min(bytesCount, (int)(fs.Length - startByte))];
			var bytesRead = fs.Read(buffer, 0, buffer.Length);

			var lines = new List<string>();

			for (int i = 0; i < bytesRead; i += bytesPerLine)
			{
				var chunk = Math.Min(bytesPerLine, bytesRead - i);

				var hex = BitConverter.ToString(buffer, i, chunk).Replace("-", " ").PadRight(48);

				var ascii = new char[chunk];
				for (int j = 0; j < chunk; j++)
				{
					var b = buffer[i + j];
					ascii[j] = b is >= 32 and <= 126 ? (char)b : '.';
				}

				lines.Add($"{hex}  {new string(ascii)}");
			}

			return (lines, bytesRead);
		}
	}
}