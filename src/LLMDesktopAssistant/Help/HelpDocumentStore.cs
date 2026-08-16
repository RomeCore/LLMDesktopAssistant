using System.Reflection;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Help;

/// <summary>
/// Loads the built-in help documentation from embedded resources and builds the document tree.
/// Documents are embedded with the logical name prefix <c>HelpDocs.</c>. Localized variants use
/// the <c>name.&lt;locale&gt;.md</c> naming convention (for example <c>chat.ru-RU.md</c>) and fall
/// back to the neutral document when the current locale has no variant.
/// </summary>
[Service]
public partial class HelpDocumentStore
{
	/// <summary>
	/// The prefix of the embedded help documentation resources.
	/// </summary>
	public const string ResourcePrefix = "HelpDocs.";

	private readonly Assembly _assembly;
	private readonly Dictionary<string, Dictionary<string, string>> _contentByPath = new(StringComparer.Ordinal);

	/// <summary>
	/// Gets the root node of the documentation tree. The root itself is a category and is
	/// typically not shown; use <see cref="Root"/>.Children as the tree items source.
	/// </summary>
	public HelpDocumentNode Root { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="HelpDocumentStore"/> class.
	/// </summary>
	/// <param name="assembly">The assembly whose embedded help resources are loaded, or <see langword="null"/> to use the current assembly.</param>
	public HelpDocumentStore(Assembly? assembly = null)
	{
		_assembly = assembly ?? Assembly.GetExecutingAssembly();
		Root = new HelpDocumentNode(string.Empty, null);
		LoadResources();
	}

	/// <summary>
	/// Gets the content of the document at the given path for the given locale, falling back
	/// to the neutral locale when the localized variant is missing.
	/// </summary>
	/// <param name="documentPath">The document path (for example <c>chat/tools</c>).</param>
	/// <param name="locale">The locale code (for example <c>ru-RU</c>), or an empty string for the neutral locale.</param>
	/// <returns>The document content, or <see langword="null"/> if the document does not exist.</returns>
	public string? GetContent(string documentPath, string locale)
	{
		if (!_contentByPath.TryGetValue(documentPath, out var byLocale))
			return null;

		if (byLocale.TryGetValue(locale, out var content))
			return content;

		if (!string.IsNullOrEmpty(locale) && byLocale.TryGetValue(string.Empty, out content))
			return content;

		return null;
	}

	/// <summary>
	/// Updates the titles of all nodes for the given locale. Called when the application language changes.
	/// </summary>
	/// <param name="locale">The locale code (for example <c>ru-RU</c>), or an empty string for the neutral locale.</param>
	public void UpdateTitles(string locale)
	{
		foreach (var child in Root.Children)
			UpdateTitles(child, locale);
	}

	private void UpdateTitles(HelpDocumentNode node, string locale)
	{
		node.SetTitle(GetTitle(node, locale));
		foreach (var child in node.Children)
			UpdateTitles(child, locale);
	}

	private string GetTitle(HelpDocumentNode node, string locale)
	{
		if (node.DocumentPath is { } path
			&& _contentByPath.TryGetValue(path, out var byLocale))
		{
			if (byLocale.TryGetValue(locale, out var content) && TryGetHeading(content, out var heading))
				return heading;
			if (!string.IsNullOrEmpty(locale)
				&& byLocale.TryGetValue(string.Empty, out content)
				&& TryGetHeading(content, out heading))
				return heading;
		}

		var name = node.NodePath.Split('/')[^1];
		return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];
	}

	private static bool TryGetHeading(string content, out string heading)
	{
		var match = HeadingRegex().Match(content);
		heading = match.Success ? match.Groups[1].Value.Trim() : string.Empty;
		return match.Success;
	}

	private void LoadResources()
	{
		foreach (var resourceName in _assembly.GetManifestResourceNames()
			.Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && n.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			.OrderBy(n => n, StringComparer.Ordinal))
		{
			using var stream = _assembly.GetManifestResourceStream(resourceName);
			if (stream == null)
				continue;

			using var reader = new StreamReader(stream);
			var content = reader.ReadToEnd();

			if (!TryParseResourceName(resourceName, out var path, out var locale))
				continue;

			if (!_contentByPath.TryGetValue(path, out var byLocale))
			{
				byLocale = new Dictionary<string, string>(StringComparer.Ordinal);
				_contentByPath[path] = byLocale;
			}

			byLocale[locale] = content;
		}

		BuildTree();
	}

	private static bool TryParseResourceName(string resourceName, out string path, out string locale)
	{
		var relative = resourceName[ResourcePrefix.Length..].Replace('\\', '/');
		var segments = relative.Split('/');
		var fileName = segments[^1];
		var baseName = fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			? fileName[..^3]
			: fileName;

		var match = LocaleSuffixRegex().Match(baseName);
		if (match.Success)
		{
			baseName = match.Groups[1].Value;
			locale = match.Groups[2].Value;
		}
		else
		{
			locale = string.Empty;
		}

		segments[^1] = baseName;
		path = string.Join('/', segments);
		return path.Length > 0;
	}

	private void BuildTree()
	{
		foreach (var path in _contentByPath.Keys.OrderBy(p => p, StringComparer.Ordinal))
		{
			var segments = path.Split('/');
			var node = Root;
			var currentPath = string.Empty;

			for (var i = 0; i < segments.Length; i++)
			{
				currentPath = currentPath.Length == 0 ? segments[i] : currentPath + '/' + segments[i];
				var child = node.Children.FirstOrDefault(c => c.NodePath == currentPath);
				if (child == null)
				{
					child = new HelpDocumentNode(currentPath, i == segments.Length - 1 ? currentPath : null);
					node.Children.Add(child);
				}
				else if (i == segments.Length - 1)
				{
					// The same node was created earlier as a category (a folder with documents),
					// but now we know it has a document of its own.
					child.SetDocument();
				}

				node = child;
			}
		}
	}

	[GeneratedRegex(@"^\s*#\s+(.+?)\s*$", RegexOptions.Multiline)]
	private static partial Regex HeadingRegex();

	[GeneratedRegex(@"^(.+)\.([a-z]{2}(?:-[A-Z]{2})?)$")]
	private static partial Regex LocaleSuffixRegex();
}
