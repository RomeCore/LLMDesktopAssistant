using System.Reflection;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tests.Localization;

public class LocFileLocalizationManagerTests : IDisposable
{
	private readonly string _userLocaleDirectory;

	public LocFileLocalizationManagerTests()
	{
		_userLocaleDirectory = Path.Combine(Path.GetTempPath(), "locfile_tests_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_userLocaleDirectory);
	}

	public void Dispose()
	{
		if (Directory.Exists(_userLocaleDirectory))
			Directory.Delete(_userLocaleDirectory, recursive: true);
	}

	private LocFileLocalizationManager CreateManager()
	{
		return new LocFileLocalizationManager(
			userLocaleDirectory: _userLocaleDirectory,
			resourceAssembly: Assembly.GetExecutingAssembly());
	}

	private void WriteUserFile(string fileName, string content)
	{
		File.WriteAllText(Path.Combine(_userLocaleDirectory, fileName), content);
	}

	[Fact]
	public void LoadsEmbeddedLocaleFiles()
	{
		AppSettingsLock.Lock(_ =>
		{
			var manager = CreateManager();
			manager.CurrentLanguage = "ru-RU";

			Assert.Equal("Привет из embedded", manager.Localize("embedded.hello"));
		});
	}

	[Fact]
	public void UserFilesOverrideEmbeddedFiles()
	{
		AppSettingsLock.Lock(_ =>
		{
			WriteUserFile("override.loc", """
				%locale: ru-RU
				%namespace: embedded

				hello: Привет от пользователя
				""");

			var manager = CreateManager();
			manager.CurrentLanguage = "ru-RU";

			Assert.Equal("Привет от пользователя", manager.Localize("embedded.hello"));
		});
	}

	[Fact]
	public void UserFilesAddNewKeys()
	{
		AppSettingsLock.Lock(_ =>
		{
			WriteUserFile("custom.loc", """
				%locale: ru-RU
				%namespace: custom

				greeting: Привет, мир!
				""");

			var manager = CreateManager();
			manager.CurrentLanguage = "ru-RU";

			Assert.Equal("Привет, мир!", manager.Localize("custom.greeting"));
		});
	}

	[Fact]
	public void FallsBackToNeutralLocale()
	{
		AppSettingsLock.Lock(_ =>
		{
			WriteUserFile("neutral.loc", """
				%locale:
				%namespace: common

				save: Save
				""");

			WriteUserFile("russian.loc", """
				%locale: ru-RU
				%namespace: common

				add: Добавить
				""");

			var manager = CreateManager();
			manager.CurrentLanguage = "ru-RU";

			Assert.Equal("Добавить", manager.Localize("common.add"));
			Assert.Equal("Save", manager.Localize("common.save"));
		});
	}

	[Fact]
	public void MissingKeyReturnsKeyItself()
	{
		AppSettingsLock.Lock(_ =>
		{
			var manager = CreateManager();

			Assert.Equal("some.missing.key", manager.Localize("some.missing.key"));
		});
	}

	[Fact]
	public void GetAvailableLanguages_ContainsLocFileLanguages()
	{
		AppSettingsLock.Lock(_ =>
		{
			var manager = CreateManager();

			var languages = manager.GetAvailableLanguages().ToArray();

			Assert.Contains("ru-RU", languages);
		});
	}

	[Fact]
	public void ChangeLanguage_UpdatesLocalization()
	{
		AppSettingsLock.Lock(_ =>
		{
			WriteUserFile("russian.loc", """
				%locale: ru-RU
				%namespace: common

				save: Сохранить
				""");

			WriteUserFile("neutral.loc", """
				%locale:
				%namespace: common

				save: Save
				""");

			var manager = CreateManager();

			manager.CurrentLanguage = "ru-RU";
			Assert.Equal("Сохранить", manager.Localize("common.save"));

			manager.CurrentLanguage = string.Empty;
			Assert.Equal("Save", manager.Localize("common.save"));
		});
	}

	[Fact]
	public void BrokenUserFileIsSkipped()
	{
		AppSettingsLock.Lock(_ =>
		{
			WriteUserFile("broken.loc", "this is not valid");

			WriteUserFile("good.loc", """
				%locale: ru-RU
				%namespace: common

				save: Сохранить
				""");

			var manager = CreateManager();
			manager.CurrentLanguage = "ru-RU";

			Assert.Equal("Сохранить", manager.Localize("common.save"));
		});
	}
}
