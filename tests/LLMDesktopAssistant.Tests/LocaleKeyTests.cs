using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tests;

public class LocaleKeyTests
{
	private sealed class TestLocalizationManager : LocalizationManager
	{
		private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal)
		{
			["test.hello"] = "Hello",
			["test.count"] = "Count: {0}"
		};

		public override string? TryLocalize(string key) => _values.GetValueOrDefault(key);

		public override IEnumerable<string> GetAvailableLanguages() => ["en-US", "ru-RU"];

		protected override bool TryChangeLanguage(string language) => true;
	}

	private static readonly TestLocalizationManager Manager = new();

	static LocaleKeyTests()
	{
		LocalizationManager.SetOverrideManager(Manager);
	}

	[Fact]
	public void GetOrCreate_ReturnsCachedInstanceForSameKey()
	{
		var first = Locale.GetKey("test.hello");
		var second = Locale.GetKey("test.hello");

		Assert.Same(first, second);
	}

	[Fact]
	public void GetOrCreate_ReturnsDifferentInstancesForDifferentKeys()
	{
		var first = Locale.GetKey("test.hello");
		var second = Locale.GetKey("test.count");

		Assert.NotSame(first, second);
	}

	[Fact]
	public void Value_ReturnsLocalizedString()
	{
		var key = Locale.GetKey("test.hello");

		Assert.Equal("Hello", key.Value);
	}

	[Fact]
	public void Value_ReturnsKeyWhenNotLocalized()
	{
		var key = Locale.GetKey("test.missing");

		Assert.Equal("test.missing", key.Value);
	}

	[Fact]
	public void Format_FormatsLocalizedValue()
	{
		var key = Locale.GetKey("test.count");

		Assert.Equal("Count: 42", key.Format(42));
	}

	[Fact]
	public void ToString_ReturnsLocalizedValue()
	{
		var key = Locale.GetKey("test.hello");

		Assert.Equal("Hello", key.ToString());
	}

	[Fact]
	public void Equals_ComparesByKey()
	{
		var first = Locale.GetKey("test.hello");
		var second = Locale.GetKey("test.hello");

		Assert.Equal(first, second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
	}

	[Fact]
	public void LanguageChange_RaisesPropertyChangedForValue()
	{
		var key = Locale.GetKey("test.hello");
		var raised = false;

		key.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(LocaleKey.Value))
				raised = true;
		};

		Manager.CurrentLanguage = "ru-RU";

		Assert.True(raised);
	}
}
