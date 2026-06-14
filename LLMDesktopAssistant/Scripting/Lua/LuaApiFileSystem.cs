using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LLMDesktopAssistant.Services.Instances;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for filesystem operations: <c>fs.*</c>.
	/// All paths are resolved relative to the chat's working directory.
	/// </summary>
	[LuaApi]
	public class LuaApiFileSystem : LuaApiBase
	{
		public override string? Namespace => "fs";

		public override string? Manuals => """
			--- fs — filesystem operations API

			Provides safe filesystem access within the chat's working directory.
			All paths are relative to the working directory.
			Path traversal outside the working directory is blocked.

			FUNCTIONS:

			--- fs.read(path)
			  Reads a text file and returns its full content as a string.
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: string

			--- fs.read_lines(path)
			  Reads a text file and returns lines as an array of strings.
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: table — array of strings

			--- fs.read_binary(path)
			  Reads a binary file and returns bytes as an array of numbers (0-255).
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: table — array of integers

			--- fs.write(path, content)
			  Writes text content to a file (overwrites if exists).
			  Parameters:
			    - path: string — path relative to working directory
			    - content: string — text content to write
			  Returns: nil

			--- fs.write_binary(path, bytes)
			  Writes binary content to a file (overwrites if exists).
			  Parameters:
			    - path: string — path relative to working directory
			    - bytes: table — array of integers (0-255)
			  Returns: nil

			--- fs.append(path, content)
			  Appends text content to the end of a file.
			  Parameters:
			    - path: string — path relative to working directory
			    - content: string — text content to append
			  Returns: nil

			--- fs.exists(path)
			  Checks if a file or directory exists.
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: boolean

			--- fs.is_file(path)
			  Returns true if the path points to an existing file.
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: boolean

			--- fs.is_dir(path)
			  Returns true if the path points to an existing directory.
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: boolean

			--- fs.list(path)
			  Lists the contents of a directory.
			  Parameters:
			    - path: string — directory path relative to working directory (default: ".")
			  Returns: table — array of entry names (strings)

			--- fs.detail(path)
			  Returns detailed information about a file or directory.
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: table or nil
			  Result fields:
			    - name: string — file/directory name
			    - path: string — absolute path
			    - is_file: boolean
			    - is_dir: boolean
			    - size: number — file size in bytes (0 for directories)
			    - created: string — creation time (ISO format)
			    - modified: string — last write time (ISO format)

			--- fs.size(path)
			  Returns the size of a file in bytes.
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: number or nil

			--- fs.copy(src, dest)
			  Copies a file or directory.
			  Parameters:
			    - src: string — source path
			    - dest: string — destination path
			  Returns: nil

			--- fs.move(src, dest)
			  Moves/renames a file or directory.
			  Parameters:
			    - src: string — source path
			    - dest: string — destination path
			  Returns: nil

			--- fs.delete(path)
			  Deletes a file.
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: nil

			--- fs.delete_dir(path)
			  Deletes a directory and all its contents.
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: nil

			--- fs.create_dir(path)
			  Creates a directory (and any missing parent directories).
			  Parameters:
			    - path: string — path relative to working directory
			  Returns: nil

			--- fs.join(...)
			  Joins path components with the platform separator.
			  Parameters:
			    - ...: string or number — one or more path components
			  Returns: string

			--- fs.dirname(path)
			  Returns the parent directory of the given path.
			  Parameters:
			    - path: string — path
			  Returns: string

			--- fs.basename(path)
			  Returns the file/directory name from a path.
			  Parameters:
			    - path: string — path
			  Returns: string

			--- fs.extname(path)
			  Returns the file extension (with dot).
			  Parameters:
			    - path: string — path
			  Returns: string or empty string

			--- fs.abs(path)
			  Resolves path to an absolute path inside the working directory.
			  Parameters:
			    - path: string — relative path
			  Returns: string or nil if outside working directory

			EXAMPLES:

			  -- Read a file
			  local content = fs.read("README.md")
			  print(content)

			  -- List directory
			  for _, name in ipairs(fs.list(".")) do
			    print(name)
			  end

			  -- Check and write
			  if not fs.exists("output") then
			    fs.create_dir("output")
			  end
			  fs.write("output/data.txt", "hello world")

			  -- File info
			  local info = fs.detail("README.md")
			  print(info.size, info.modified)
			""";

		private readonly FileAccessService _fileAccess;

		public LuaApiFileSystem(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["read"] = DynValue.NewCallback(new CallbackFunction(Read));
			ns["read_lines"] = DynValue.NewCallback(new CallbackFunction(ReadLines));
			ns["read_binary"] = DynValue.NewCallback(new CallbackFunction(ReadBinary));
			ns["write"] = DynValue.NewCallback(new CallbackFunction(Write));
			ns["write_binary"] = DynValue.NewCallback(new CallbackFunction(WriteBinary));
			ns["append"] = DynValue.NewCallback(new CallbackFunction(Append));
			ns["exists"] = DynValue.NewCallback(new CallbackFunction(Exists));
			ns["is_file"] = DynValue.NewCallback(new CallbackFunction(IsFile));
			ns["is_dir"] = DynValue.NewCallback(new CallbackFunction(IsDir));
			ns["list"] = DynValue.NewCallback(new CallbackFunction(List));
			ns["detail"] = DynValue.NewCallback(new CallbackFunction(Detail));
			ns["size"] = DynValue.NewCallback(new CallbackFunction(Size));
			ns["copy"] = DynValue.NewCallback(new CallbackFunction(Copy));
			ns["move"] = DynValue.NewCallback(new CallbackFunction(Move));
			ns["delete"] = DynValue.NewCallback(new CallbackFunction(Delete));
			ns["delete_dir"] = DynValue.NewCallback(new CallbackFunction(DeleteDir));
			ns["create_dir"] = DynValue.NewCallback(new CallbackFunction(CreateDir));
			ns["join"] = DynValue.NewCallback(new CallbackFunction(Join));
			ns["dirname"] = DynValue.NewCallback(new CallbackFunction(DirName));
			ns["basename"] = DynValue.NewCallback(new CallbackFunction(BaseName));
			ns["extname"] = DynValue.NewCallback(new CallbackFunction(ExtName));
			ns["abs"] = DynValue.NewCallback(new CallbackFunction(Abs));
		}

		private DynValue Read(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.read(path): at least 1 argument expected.");
			var path = GetPath(args, 0);
			return DynValue.NewString(File.ReadAllText(path));
		}

		private DynValue ReadLines(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.read_lines(path): at least 1 argument expected.");
			var path = GetPath(args, 0);
			var lines = File.ReadAllLines(path);
			var result = new Table(ctx.OwnerScript);
			for (int i = 0; i < lines.Length; i++)
				result[i + 1] = DynValue.NewString(lines[i]);
			return DynValue.NewTable(result);
		}

		private DynValue ReadBinary(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.read_binary(path): at least 1 argument expected.");
			var path = GetPath(args, 0);
			var bytes = File.ReadAllBytes(path);
			var result = new Table(ctx.OwnerScript);
			for (int i = 0; i < bytes.Length; i++)
				result[i + 1] = DynValue.NewNumber(bytes[i]);
			return DynValue.NewTable(result);
		}

		private DynValue Write(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("fs.write(path, content): at least 2 arguments expected.");
			var path = GetPath(args, 0);
			var content = args[1].CastToString();
			if (content == null)
				throw new ScriptRuntimeException("fs.write(): second argument must be a string.");
			File.WriteAllText(path, content);
			return DynValue.Nil;
		}

		private DynValue WriteBinary(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("fs.write_binary(path, bytes): at least 2 arguments expected.");
			var path = GetPath(args, 0);
			var bytesTable = args[1];
			if (bytesTable.Type != DataType.Table)
				throw new ScriptRuntimeException("fs.write_binary(): second argument must be a table of bytes.");
			var bytes = new List<byte>();
			foreach (var kv in bytesTable.Table.Pairs)
			{
				if (kv.Value.Type == DataType.Number)
					bytes.Add((byte)kv.Value.Number);
			}
			File.WriteAllBytes(path, [.. bytes]);
			return DynValue.Nil;
		}

		private DynValue Append(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("fs.append(path, content): at least 2 arguments expected.");
			var path = GetPath(args, 0);
			var content = args[1].CastToString();
			if (content == null)
				throw new ScriptRuntimeException("fs.append(): second argument must be a string.");
			File.AppendAllText(path, content);
			return DynValue.Nil;
		}

		private DynValue Exists(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.exists(path): at least 1 argument expected.");
			var path = args[0].CastToString();
			if (path == null)
				throw new ScriptRuntimeException("fs.exists(): first argument must be a string.");
			var fullPath = _fileAccess.TryAccessPath(path);
			return DynValue.NewBoolean(fullPath != null && (File.Exists(fullPath) || Directory.Exists(fullPath)));
		}

		private DynValue IsFile(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.is_file(path): at least 1 argument expected.");
			var path = args[0].CastToString();
			if (path == null)
				throw new ScriptRuntimeException("fs.is_file(): first argument must be a string.");
			var fullPath = _fileAccess.TryAccessPath(path);
			return DynValue.NewBoolean(fullPath != null && File.Exists(fullPath));
		}

		private DynValue IsDir(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.is_dir(path): at least 1 argument expected.");
			var path = args[0].CastToString();
			if (path == null)
				throw new ScriptRuntimeException("fs.is_dir(): first argument must be a string.");
			var fullPath = _fileAccess.TryAccessPath(path);
			return DynValue.NewBoolean(fullPath != null && Directory.Exists(fullPath));
		}

		private DynValue List(ScriptExecutionContext ctx, CallbackArguments args)
		{
			var path = args.Count > 0 ? (args[0].CastToString() ?? ".") : ".";
			var fullPath = _fileAccess.TryAccessPath(path);
			if (fullPath == null)
				throw new ScriptRuntimeException($"fs.list(): path '{path}' is outside working directory.");
			if (!Directory.Exists(fullPath))
				throw new ScriptRuntimeException($"fs.list(): directory '{path}' does not exist.");

			var entries = Directory.GetFileSystemEntries(fullPath);
			var result = new Table(ctx.OwnerScript);
			for (int i = 0; i < entries.Length; i++)
				result[i + 1] = DynValue.NewString(Path.GetFileName(entries[i]));
			return DynValue.NewTable(result);
		}

		private DynValue Detail(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.detail(path): at least 1 argument expected.");
			var path = args[0].CastToString();
			if (path == null)
				throw new ScriptRuntimeException("fs.detail(): first argument must be a string.");
			var fullPath = _fileAccess.TryAccessPath(path);
			if (fullPath == null)
				return DynValue.Nil;

			var t = new Table(ctx.OwnerScript);
			t["name"] = DynValue.NewString(Path.GetFileName(fullPath));
			t["path"] = DynValue.NewString(fullPath);
			t["is_file"] = DynValue.NewBoolean(File.Exists(fullPath));
			t["is_dir"] = DynValue.NewBoolean(Directory.Exists(fullPath));

			try
			{
				var info = new FileInfo(fullPath);
				if (info.Exists)
				{
					t["size"] = DynValue.NewNumber(info.Length);
					t["created"] = DynValue.NewString(info.CreationTimeUtc.ToString("O"));
					t["modified"] = DynValue.NewString(info.LastWriteTimeUtc.ToString("O"));
				}
				else
				{
					var dirInfo = new DirectoryInfo(fullPath);
					if (dirInfo.Exists)
					{
						t["size"] = DynValue.NewNumber(0);
						t["created"] = DynValue.NewString(dirInfo.CreationTimeUtc.ToString("O"));
						t["modified"] = DynValue.NewString(dirInfo.LastWriteTimeUtc.ToString("O"));
					}
				}
			}
			catch
			{
				// ignore
			}

			return DynValue.NewTable(t);
		}

		private DynValue Size(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.size(path): at least 1 argument expected.");
			var path = args[0].CastToString();
			if (path == null)
				throw new ScriptRuntimeException("fs.size(): first argument must be a string.");
			var fullPath = _fileAccess.TryAccessPath(path);
			if (fullPath == null || !File.Exists(fullPath))
				return DynValue.Nil;
			return DynValue.NewNumber(new FileInfo(fullPath).Length);
		}

		private DynValue Copy(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("fs.copy(src, dest): at least 2 arguments expected.");
			var src = GetPath(args, 0);
			var dest = GetPath(args, 1);

			if (Directory.Exists(src))
				CopyDirectoryRecursive(src, dest);
			else
				File.Copy(src, dest, overwrite: true);

			return DynValue.Nil;
		}

		private DynValue Move(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("fs.move(src, dest): at least 2 arguments expected.");
			var src = GetPath(args, 0);
			var dest = GetPath(args, 1);

			if (Directory.Exists(src))
				Directory.Move(src, dest);
			else
				File.Move(src, dest, overwrite: true);

			return DynValue.Nil;
		}

		private DynValue Delete(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.delete(path): at least 1 argument expected.");
			var path = GetPath(args, 0);
			File.Delete(path);
			return DynValue.Nil;
		}

		private DynValue DeleteDir(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.delete_dir(path): at least 1 argument expected.");
			var path = GetPath(args, 0);
			Directory.Delete(path, recursive: true);
			return DynValue.Nil;
		}

		private DynValue CreateDir(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.create_dir(path): at least 1 argument expected.");
			var path = GetPath(args, 0);
			Directory.CreateDirectory(path);
			return DynValue.Nil;
		}

		private static DynValue Join(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.join(...): at least 1 argument expected.");
			var parts = new string[args.Count];
			for (int i = 0; i < args.Count; i++)
			{
				var s = args[i].CastToString();
				if (s == null)
					throw new ScriptRuntimeException($"fs.join(): argument {i + 1} must be a string or number.");
				parts[i] = s;
			}
			return DynValue.NewString(Path.Combine(parts));
		}

		private static DynValue DirName(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.dirname(path): at least 1 argument expected.");
			var path = args[0].CastToString();
			if (path == null)
				throw new ScriptRuntimeException("fs.dirname(): first argument must be a string.");
			return DynValue.NewString(Path.GetDirectoryName(path) ?? "");
		}

		private static DynValue BaseName(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.basename(path): at least 1 argument expected.");
			var path = args[0].CastToString();
			if (path == null)
				throw new ScriptRuntimeException("fs.basename(): first argument must be a string.");
			return DynValue.NewString(Path.GetFileName(path));
		}

		private static DynValue ExtName(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.extname(path): at least 1 argument expected.");
			var path = args[0].CastToString();
			if (path == null)
				throw new ScriptRuntimeException("fs.extname(): first argument must be a string.");
			return DynValue.NewString(Path.GetExtension(path));
		}

		private DynValue Abs(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("fs.abs(path): at least 1 argument expected.");
			var path = args[0].CastToString();
			if (path == null)
				throw new ScriptRuntimeException("fs.abs(): first argument must be a string.");
			var full = _fileAccess.TryAccessPath(path);
			if (full == null)
				return DynValue.Nil;
			return DynValue.NewString(full);
		}

		// --- Helpers ---

		private string GetPath(CallbackArguments args, int index)
		{
			var path = args[index].CastToString();
			if (path == null)
				throw new ScriptRuntimeException($"Argument {index + 1} must be a string (path).");
			return _fileAccess.AccessPath(path);
		}

		private static void CopyDirectoryRecursive(string source, string dest)
		{
			Directory.CreateDirectory(dest);
			foreach (var file in Directory.GetFiles(source))
			{
				var destFile = Path.Combine(dest, Path.GetFileName(file));
				File.Copy(file, destFile, overwrite: true);
			}
			foreach (var dir in Directory.GetDirectories(source))
			{
				var destDir = Path.Combine(dest, Path.GetFileName(dir));
				CopyDirectoryRecursive(dir, destDir);
			}
		}
	}
}
