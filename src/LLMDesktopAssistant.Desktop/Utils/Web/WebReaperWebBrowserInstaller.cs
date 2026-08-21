using System.IO;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Utils.Web;
using Microsoft.Extensions.Logging;
using WebReaper.Stealth.CloakBrowser;

namespace LLMDesktopAssistant.Desktop.Utils.Web;

/// <summary>
/// The WebReaper-backed <see cref="IWebBrowserInstaller"/>: installs the
/// Playwright Chromium runtime and downloads the stealth CloakBrowser binary
/// from upstream.
/// </summary>
public sealed class WebReaperWebBrowserInstaller : IWebBrowserInstaller
{
	private const string ChromiumBrowserName = "chromium";
	private const string HeadlessShellBrowserName = "chromium-headless-shell";

	private readonly ILogger _logger;

	/// <summary>
	/// Creates an installer.
	/// </summary>
	/// <param name="logger">The logger.</param>
	public WebReaperWebBrowserInstaller(ILogger logger)
	{
		_logger = logger;
	}

	/// <inheritdoc />
	public bool IsPlaywrightInstalled
	{
		get
		{
			try
			{
				var expectedDirectories = GetExpectedBrowserDirectories();
				if (expectedDirectories.Count == 0)
					return HasAnyInstalledBrowserFallback();

				var root = GetBrowsersRoot();
				return expectedDirectories.All(directory => HasExecutable(Path.Combine(root, directory)));
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "WebReaperWebBrowserInstaller: failed to check whether Playwright is installed.");
				return false;
			}
		}
	}

	/// <inheritdoc />
	public bool IsCloakBrowserInstalled
	{
		get
		{
			var cacheDir = Path.Combine(CloakBrowserInstaller.GetWebReaperHome(), "stealth", "cloakbrowser");
			return Directory.Exists(cacheDir) && Directory.EnumerateDirectories(cacheDir)
				.Any(versionDir => File.Exists(Path.Combine(versionDir, "chrome.exe")));
		}
	}

	/// <inheritdoc />
	public Task InstallPlaywrightAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => Microsoft.Playwright.Program.Main(["install", "chromium"]), cancellationToken);

	/// <inheritdoc />
	public Task InstallCloakBrowserAsync(CancellationToken cancellationToken = default)
		=> CloakBrowserInstaller.EnsureInstalledAsync(
			new CloakBrowserOptions { AutoInstall = AutoInstallPolicy.NoPromptYes }, _logger, cancellationToken);

	/// <summary>
	/// Resolves the browser directories expected by the current Playwright
	/// package from the driver's browsers.json. Directory names use
	/// underscores because Playwright replaces '-' with '_' when naming
	/// browser folders.
	/// </summary>
	/// <returns>The expected relative browser directory names.</returns>
	private static IReadOnlyList<string> GetExpectedBrowserDirectories()
	{
		var browsersJsonPath = Path.Combine(AppContext.BaseDirectory, ".playwright", "package", "browsers.json");
		if (!File.Exists(browsersJsonPath))
			return Array.Empty<string>();

		var root = JsonNode.Parse(File.ReadAllText(browsersJsonPath));
		var browsers = root?["browsers"]?.AsArray();
		if (browsers is null)
			return Array.Empty<string>();

		var result = new List<string>();
		foreach (var browser in browsers)
		{
			var name = (string?)browser?["name"];
			var revision = (string?)browser?["revision"];
			if ((name is ChromiumBrowserName or HeadlessShellBrowserName) && revision is not null)
				result.Add($"{name.Replace('-', '_')}-{revision}");
		}

		return result;
	}

	/// <summary>
	/// Gets the Playwright browsers root directory, honoring the
	/// PLAYWRIGHT_BROWSERS_PATH environment variable when set.
	/// </summary>
	/// <returns>The absolute path of the browsers root directory.</returns>
	private static string GetBrowsersRoot()
	{
		var environmentPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
		if (!string.IsNullOrWhiteSpace(environmentPath) && environmentPath != "0")
			return environmentPath;

		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ms-playwright");
	}

	/// <summary>
	/// Determines whether a directory exists and contains at least one
	/// executable file, i.e. the browser runtime was actually materialized.
	/// </summary>
	/// <param name="directory">The directory to probe.</param>
	/// <returns><see langword="true"/> if the directory contains an executable file.</returns>
	private static bool HasExecutable(string directory)
		=> Directory.Exists(directory) && Directory.EnumerateFiles(directory, "*.exe", SearchOption.AllDirectories).Any();

	/// <summary>
	/// Fallback heuristic used when browsers.json cannot be read: both a
	/// chromium and a headless-shell directory with executables must exist.
	/// </summary>
	/// <returns><see langword="true"/> if both runtimes appear to be installed.</returns>
	private bool HasAnyInstalledBrowserFallback()
	{
		var root = GetBrowsersRoot();
		if (!Directory.Exists(root))
			return false;

		return Directory.EnumerateDirectories(root, "chromium-*").Any(HasExecutable)
			&& Directory.EnumerateDirectories(root, "chromium_headless_shell-*").Any(HasExecutable);
	}
}
