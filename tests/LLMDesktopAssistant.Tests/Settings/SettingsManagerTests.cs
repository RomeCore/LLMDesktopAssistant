using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.Tests.Settings;

public class SettingsManagerTests
{
	[SettingsObject("test-manager-settings")]
	private sealed class TestSettings : SettingsObject
	{
	}

	[Fact]
	public void TryGetReturnsFalseForMissingId()
	{
		Assert.False(SettingsManager.TryGet<TestSettings>("missing-id", out var settings));
		Assert.Null(settings);
	}

	[Fact]
	public void TryGetReturnsExistingInstance()
	{
		var created = SettingsManager.Get<TestSettings>("existing-id");

		Assert.True(SettingsManager.TryGet<TestSettings>("existing-id", out var settings));
		Assert.Same(created, settings);

		SettingsManager.Remove<TestSettings>("existing-id");
		SettingsManager.GetCategory<TestSettings>().Save();
	}

	[Fact]
	public void TryGetDefaultIdOverload()
	{
		Assert.False(SettingsManager.TryGet<TestSettings>(out _));

		var created = SettingsManager.Get<TestSettings>();

		Assert.True(SettingsManager.TryGet<TestSettings>(out var settings));
		Assert.Same(created, settings);

		SettingsManager.Remove<TestSettings>();
		SettingsManager.GetCategory<TestSettings>().Save();
	}

	[Fact]
	public void TryGetDoesNotCreateInstance()
	{
		Assert.False(SettingsManager.TryGet<TestSettings>("never-created", out _));
		Assert.False(SettingsManager.TryGet<TestSettings>("never-created", out _));
	}
}
