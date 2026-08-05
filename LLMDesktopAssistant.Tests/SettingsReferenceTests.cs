using System.Text.Json;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.Tests;

public class SettingsReferenceTests
{
	[SettingsObject("test-settings")]
	private sealed class TestSettings : SettingsObject
	{
		public string? Value { get; set; }

		public SettingsReference<TestSettings>? Reference { get; set; }
	}

	private sealed class RequiredReferenceSettings : SettingsObject
	{
		public required SettingsReference<TestSettings> Reference { get; init; }
	}

	[Fact]
	public void SerializesAsId()
	{
		var reference = new SettingsReference<TestSettings> { Id = "profile-a" };

		var json = JsonSerializer.Serialize(reference, SettingsManager.jsonOptions);

		Assert.Equal("\"profile-a\"", json);
	}

	[Fact]
	public void DeserializesByIdAndResolvesObjectLazily()
	{
		var reference = JsonSerializer.Deserialize<SettingsReference<TestSettings>>(
			"\"profile-b\"", SettingsManager.jsonOptions);

		Assert.NotNull(reference);
		Assert.Equal("profile-b", reference!.Id);
		Assert.Same(SettingsManager.Get<TestSettings>("profile-b"), reference.Object);
	}

	[Fact]
	public void RoundTripsThroughSettingsCategory()
	{
		var settings = SettingsManager.Get<TestSettings>("profile-c");
		settings.Value = "hello";
		settings.Reference = new SettingsReference<TestSettings> { Id = "profile-d" };

		var category = SettingsManager.GetCategory<TestSettings>();
		var json = JsonSerializer.Serialize(
			category.GetAvailableIds().ToDictionary(id => id, id => SettingsManager.Get<TestSettings>(id)),
			SettingsManager.jsonOptions);

		var deserialized = JsonSerializer.Deserialize<Dictionary<string, TestSettings>>(json, SettingsManager.jsonOptions)!;

		var reference = deserialized["profile-c"].Reference!;
		Assert.Equal("profile-d", reference.Id);
		Assert.Same(SettingsManager.Get<TestSettings>("profile-d"), reference.Object);

		SettingsManager.Remove<TestSettings>("profile-c");
		SettingsManager.Remove<TestSettings>("profile-d");
		SettingsManager.GetCategory<TestSettings>().Save();
	}

	[Fact]
	public void SerializesRequiredInitPropertyAsId()
	{
		var settings = new RequiredReferenceSettings
		{
			Reference = new SettingsReference<TestSettings> { Id = "required-profile" }
		};

		var json = JsonSerializer.Serialize(settings, SettingsManager.jsonOptions);

		Assert.Contains("\"reference\": \"required-profile\"", json);
	}

	[Fact]
	public void DeserializesRequiredInitPropertyById()
	{
		var settings = JsonSerializer.Deserialize<RequiredReferenceSettings>(
			"""{"reference":"required-profile"}""", SettingsManager.jsonOptions);

		Assert.NotNull(settings);
		Assert.Equal("required-profile", settings!.Reference.Id);
		Assert.Same(SettingsManager.Get<TestSettings>("required-profile"), settings.Reference.Object);

		SettingsManager.Remove<TestSettings>("required-profile");
		SettingsManager.GetCategory<TestSettings>().Save();
	}

	[Fact]
	public void DeserializingWithoutRequiredInitPropertyThrows()
	{
		Assert.Throws<JsonException>(() =>
			JsonSerializer.Deserialize<RequiredReferenceSettings>("{}", SettingsManager.jsonOptions));
	}
}
