using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Implementations.Filesystem;
using Xunit.Abstractions;

namespace LLMDesktopAssistant.Tests.ToolModules;

/// <summary>
/// Tests for the <see cref="FilesystemEditToolModule"/> class.
/// </summary>
public class FilesystemEditToolModuleTests(ITestOutputHelper output)
{
	private sealed class TempDirFileAccess : IWorkingDirectoryAccessService
	{
		private readonly string _root;

		public TempDirFileAccess(string root) => _root = root;

		public string AccessPath(string path, DirectoryAccessMode mode) => Path.Combine(_root, path);

		public string CheckedAccessPath(string path, DirectoryAccessMode mode, out bool isAccessed)
		{
			isAccessed = true;
			return Path.Combine(_root, path);
		}

		public string GetWorkingDirectory() => _root;

		public string? TryAccessPath(string path, DirectoryAccessMode mode) => Path.Combine(_root, path);
	}

	private sealed class TempDir : IDisposable
	{
		public string Root { get; } = Path.Combine(Path.GetTempPath(), "fs-edit-tests-" + Guid.NewGuid().ToString("N"));

		public TempDir() => Directory.CreateDirectory(Root);

		public string Write(string relativePath, string content)
		{
			var fullPath = Path.Combine(Root, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
			File.WriteAllText(fullPath, content);
			return fullPath;
		}

		public void Dispose()
		{
			try
			{
				Directory.Delete(Root, true);
			}
			catch
			{
				// Best effort cleanup
			}
		}
	}

	private static JsonObject Patch(string match, string? replace = null, bool useRegex = false, bool ignoreCase = false)
	{
		var patch = new JsonObject
		{
			["match"] = match
		};
		if (replace != null)
			patch["replace"] = replace;
		if (useRegex)
			patch["useRegex"] = true;
		if (ignoreCase)
			patch["ignoreCase"] = true;
		return patch;
	}

	private static FilesystemEditToolModule CreateModule(string root) => new(new TempDirFileAccess(root));

	private static ToolInfo GetFsEditTool(FilesystemEditToolModule module)
		=> module.GetTools().Single(t => t.Name == "fs-edit");

	private static async Task<ReactiveToolResult> ExecuteAsync(ToolInfo tool, JsonObject args)
	{
		var ctx = ToolExecutionContext.CreateDummy(tool, args, null);
		return await tool.Executor!(args, ctx, CancellationToken.None);
	}

	[Fact]
	public async Task Replace_Plain_PreservesPrefixAndSuffix()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "    abc");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("ab", "de") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("    dec", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_IgnoresIndentation()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.cs", "    public class Foo {");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.cs",
			["patches"] = new JsonArray { Patch("public class Foo", "public class Bar") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("    public class Bar {", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_AllOccurrences()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo foo foo");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo", "bar") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("bar bar bar", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_IgnoreCase()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "Hello World");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("hello", "Hi", ignoreCase: true) }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("Hi World", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_MultilineMatch()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "a\nb\nc");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("a\nb", "x\ny") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("x\ny\nc", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_MultilineMatch_PreservesIndentation()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "    a\n    b\n    c");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("a\nb", "x") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("    x\n    c", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_MultilineReplacement()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "    a\n    b");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("a", "x\ny") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("    x\ny\n    b", File.ReadAllText(file));
	}

	[Fact]
	public async Task Delete_Plain_EmptyReplace()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "line1\nfoo\nline3");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo", "") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("line1\n\nline3", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Regex_CaptureGroup()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.cs", "class Foo\nclass Bar");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.cs",
			["patches"] = new JsonArray { Patch("class (\\w+)", "class Renamed_$1", useRegex: true) }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("class Renamed_Foo\nclass Renamed_Bar", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Regex_NamedGroup()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "val = 42");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("val = (?<n>\\d+)", "val = ${n}x", useRegex: true) }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("val = 42x", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_MultiLineFile_AllOccurrences()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo\nx\nfoo");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo", "bar") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("bar\nx\nbar", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_MultipleMultilineBlocks()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "a\nb\nsep\na\nb");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("a\nb", "X") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("X\nsep\nX", File.ReadAllText(file));
	}

	[Fact]
	public async Task Delete_Plain_MultilineBlock_PreservesSurroundings()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "x a\nb y");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("a\nb", "") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("x  y", File.ReadAllText(file));
	}

	[Fact]
	public async Task Patch_WithoutReplace_DeletesMatch()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo bar");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal(" bar", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_TabsInIndentation_Ignored()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.cs", "\tpublic class Foo");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.cs",
			["patches"] = new JsonArray { Patch("public class Foo", "public class Bar") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("\tpublic class Bar", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_NonOverlappingOccurrences()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "aaaa");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("aa", "b") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("bb", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_NoRecursionIntoReplacement()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo", "foofoo") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("foofoo", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_EmptyFile_NotFound()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo", "bar") }
		});

		Assert.Contains("No changes were made to the file.", result.ResultContent);
		Assert.Contains("Patch #1 (match: \"foo\"): no occurrences found.", result.ResultContent);
		Assert.Equal("", File.ReadAllText(file));
	}

	[Fact]
	public async Task Delete_Regex_EmptyReplace()
	{
		using var temp = new TempDir();

		var file = temp.Write("file.txt", "x = 1;\nfoo()\ny = 2;");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo\\(\\)", "", useRegex: true) }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("x = 1;\n\ny = 2;", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Regex_IgnoreCase()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "Hello World");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("hello", "Hi", useRegex: true, ignoreCase: true) }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("Hi World", File.ReadAllText(file));
	}

	[Fact]
	public async Task Patches_AppliedSequentially()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo bar");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo", "baz"), Patch("bar", "qux") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("baz qux", File.ReadAllText(file));
	}

	[Fact]
	public async Task Patches_MixedPlainAndRegex()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo\nclass Foo");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo", "x"), Patch("class (\\w+)", "class C_$1", useRegex: true) }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("x\nclass C_Foo", File.ReadAllText(file));
	}

	[Fact]
	public async Task Patch_NotFound_OthersStillApplied()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo", "bar"), Patch("zzz", "x") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Contains("[NOT APPLIED PATCHES]", result.ResultContent);
		Assert.Contains("Patch #2 (match: \"zzz\"): no occurrences found.", result.ResultContent);
		Assert.Equal("bar", File.ReadAllText(file));
	}

	[Fact]
	public async Task Patch_NotFound_NoChangesMade()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "abc");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("zzz", "x") }
		});

		Assert.Contains("No changes were made to the file.", result.ResultContent);
		Assert.Contains("Patch #1 (match: \"zzz\"): no occurrences found.", result.ResultContent);
		Assert.Equal("abc", File.ReadAllText(file));
	}

	[Fact]
	public async Task Patches_Empty_ReturnsError()
	{
		using var temp = new TempDir();
		temp.Write("file.txt", "abc");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray()
		});

		Assert.Contains("'patches' parameter cannot be empty.", result.ResultContent);
	}

	[Fact]
	public async Task InvalidRegex_Reported_OthersStillApplied()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("(", "x", useRegex: true), Patch("foo", "bar") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Contains("[NOT APPLIED PATCHES]", result.ResultContent);
		Assert.Contains("Patch #1 (match: \"(\"): invalid pattern:", result.ResultContent);
		Assert.Equal("bar", File.ReadAllText(file));
	}

	[Fact]
	public async Task Match_TrailingNewline_Ignored()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo\nbar");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo\n", "baz") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("baz\nbar", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_PreservesCrlfLineEndings()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", "foo\r\nbar");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray { Patch("foo", "baz") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("baz\r\nbar", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_PythonCode_IndentedMatch_IndentedReplace_NoDoubledIndent()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.py", """
			def foo():
			    if x:
			        do_something()
			    else:
			        do_other()
			""");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.py",
			["patches"] = new JsonArray { Patch("    if x:", "    if x and y:") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("""
			def foo():
			    if x and y:
			        do_something()
			    else:
			        do_other()
			""", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_PythonCode_MatchWithoutIndent_ReplaceWithIndent_UsesFileIndent()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.py", """
			def foo():
			    if x:
			        do_other()
			""");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.py",
			["patches"] = new JsonArray { Patch("if x:", "if x and y:") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("""
			def foo():
			    if x and y:
			        do_other()
			""", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_PythonCode_MultilineBlock_LineByLine_KeepsIndentation()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.py", """
			def foo():
			    if x:
			        do_something()
			    else:
			        do_other()
			""");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.py",
			["patches"] = new JsonArray
			{
				Patch("""
				    if x:
				        do_something()
				    """, """
				    if x and y:
				        do_something_else()
				    """)
			}
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("""
			def foo():
			    if x and y:
			        do_something_else()
			    else:
			        do_other()
			""", File.ReadAllText(file));
	}

	[Theory]
	[InlineData("""
		if x:
		    for i in range(3):
		        print(i)
		""", """
		if x:
		    for i in range(3):
		        print(i * 2)
		""")]
	[InlineData("""
		 if x:
		     for i in range(3):
		         print(i)
		 """, """
		 if x:
		     for i in range(3):
		         print(i * 2)
		""")]
	[InlineData("""
		  if x:
		      for i in range(3):
		          print(i)
		  """, """
		  if x:
		      for i in range(3):
		          print(i * 2)
		""")]
	[InlineData("""
		   if x:
		       for i in range(3):
		           print(i)
		   """, """
		   if x:
		       for i in range(3):
		           print(i * 2)
		""")]
	[InlineData("""
		    if x:
		        for i in range(3):
		            print(i)
		    """, """
		    if x:
		        for i in range(3):
		            print(i * 2)
		""")]
	public async Task Replace_PythonCode_NestedBlock_IgnoresSameIndentation(string match, string text)
	{
		using var temp = new TempDir();
		var file = temp.Write("file.py", """
			def foo():
			    if x:
			        for i in range(3):
			            print(i)
			""");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.py",
			["patches"] = new JsonArray
			{
				Patch(match, text)
			}
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("""
			def foo():
			    if x:
			        for i in range(3):
			            print(i * 2)
			""", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_PythonCode_DeleteBlock_ReplacedBySingleLine()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.py", """
			def foo():
			    if x:
			        do_something()
			    else:
			        do_other()
			""");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.py",
			["patches"] = new JsonArray
			{
				Patch("""
				    if x:
				        do_something()
				    else:
				        do_other()
				    """, "    pass")
			}
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("""
			def foo():
			    pass
			""", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_PythonCode_TabsIndentation_NoDoubling()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.py", "def foo():\n\tif x:\n\t\tdo_something()");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.py",
			["patches"] = new JsonArray { Patch("\tif x:", "\tif x and y:") }
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("def foo():\n\tif x and y:\n\t\tdo_something()", File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_Plain_Multiline_MatchWithIndent_ReplacementWithoutIndent()
	{
		using var temp = new TempDir();
		var file = temp.Write("file.py", """
			def foo():
			    if x:
			        do_something()
			    else:
			        do_other()
			""");

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.py",
			["patches"] = new JsonArray
			{
				Patch("""
				    if x:
				        do_something()
				    """, """
				    if z:
				        do_z()
				    """)
			}
		});

		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal("""
			def foo():
			    if z:
			        do_z()
			    else:
			        do_other()
			""", File.ReadAllText(file));
	}
	
	private async Task TestReplaceAsync(string original, string expected, params JsonObject[] patches)
	{
		using var temp = new TempDir();
		var file = temp.Write("file.txt", original);

		var result = await ExecuteAsync(GetFsEditTool(CreateModule(temp.Root)), new JsonObject
		{
			["path"] = "file.txt",
			["patches"] = new JsonArray([.. patches])
		});

		output.WriteLine(result.ResultContent);
		Assert.Contains("File edited successfully", result.ResultContent);
		Assert.Equal(expected, File.ReadAllText(file));
	}

	[Fact]
	public async Task Replace_CSharpCode_ComplexPatches()
	{
		await TestReplaceAsync("""
			namespace MyNamespace
			{
				public static class Program
				{
					public void Main()
					{
						var foo = new Foo
						{
							Property1 = "value1",
							Property2 = "value2"
						};
					}
				}
			}
			""", """
			namespace MyNamespace
			{
				public static class Program
				{
					public void Main(string[] args)
					{
						var bar = new Bar
						{
							Property3 = "value3",
							Property4 = "value4"
						};
					}
				}
			}
			""", Patch("""
			Main()
			""", """
			Main(string[] args)
			"""), Patch("""
			= new Foo
			{
				Property1 = "value1",
				Property2 = "value2
			""", """
			= new Bar
			{
				Property3 = "value3",
				Property4 = "value4
			"""), Patch("""
			var foo
			""", """
			var bar
			"""));
	}

	[Fact]
	public async Task Replace_CSharpCode_RealFailure1()
	{
		await TestReplaceAsync("""
			using System.Net;

			namespace LLMDesktopAssistant.Utils.Web
			{
				/// <summary>
				/// The result of a page fetch operation containing the raw HTML, HTTP status code, and response headers.
				/// </summary>
				public sealed record FetchResult(
					string Html,
					int? HttpStatus,
					IReadOnlyDictionary<string, string> Headers
				);

				/// <summary>
				/// Fetches web page content over HTTP with retries, cookie support, proxy support,
				/// SSL bypass, and response caching.
				/// </summary>
				public static class HtmlContentFetcher
				{
					private static readonly AsyncCache<string, FetchResult> _cache = new(
						FetchCoreAsync,
						slidingExpirationTime: TimeSpan.FromMinutes(15));
			""", """
			using System.Net;

			namespace LLMDesktopAssistant.Utils.Web
			{
			    /// <summary>
			    /// Fetches web page content over HTTP with retries, cookie support, proxy support,
			    /// SSL bypass, and response caching.
			    /// </summary>
			    public static class HtmlContentFetcher
				{
					private static readonly AsyncCache<string, FetchResult> _cache = new(
						FetchCoreAsync,
						slidingExpirationTime: TimeSpan.FromMinutes(15));
			""", Patch("""
			namespace LLMDesktopAssistant.Utils.Web
			{
			    /// <summary>
			    /// The result of a page fetch operation containing the raw HTML, HTTP status code, and response headers.
			    /// </summary>
			    public sealed record FetchResult(
			        string Html,
			        int? HttpStatus,
			        IReadOnlyDictionary<string, string> Headers
			    );
			  
			    /// <summary>
			    /// Fetches web page content over HTTP with retries, cookie support, proxy support,
			    /// SSL bypass, and response caching.
			    /// </summary>
			    public static class HtmlContentFetcher
			""", """
			namespace LLMDesktopAssistant.Utils.Web
			{
			    /// <summary>
			    /// Fetches web page content over HTTP with retries, cookie support, proxy support,
			    /// SSL bypass, and response caching.
			    /// </summary>
			    public static class HtmlContentFetcher
			"""));
	}

	[Fact]
	public void ArgumentSchema_ContainsPatchesArray()
	{
		var tool = GetFsEditTool(CreateModule(Path.GetTempPath()));

		var schema = tool.ArgumentSchema;
		var patchesSchema = schema["properties"]?["patches"] as JsonObject;
		Assert.NotNull(patchesSchema);
		Assert.Equal("array", (string?)patchesSchema!["type"]);

		output.WriteLine(schema.ToJsonString(new System.Text.Json.JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		}));

		var items = patchesSchema["items"] as JsonObject;
		Assert.NotNull(items);
		var properties = items!["properties"] as JsonObject;
		Assert.NotNull(properties);
		Assert.NotNull(properties!["match"]);
		Assert.NotNull(properties["replace"]);
		Assert.NotNull(properties["useRegex"]);
		Assert.NotNull(properties["ignoreCase"]);
	}
}
