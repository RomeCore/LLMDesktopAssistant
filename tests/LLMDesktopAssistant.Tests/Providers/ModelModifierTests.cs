using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Providers;
using RCLargeLanguageModels.Completions.Properties;

namespace LLMDesktopAssistant.Tests.Providers;

public class ModelModifierTests
{
	[Fact]
	public void ToCompletionProperties_EmptyModifier_ReturnsEmptyCollection()
	{
		var modifier = new ModelModifier();

		var properties = modifier.ToCompletionProperties().ToList();

		Assert.Empty(properties);
	}

	[Fact]
	public void ToCompletionProperties_ReasoningMaximum_ReturnsReasoningPropertyWithMaxEffort()
	{
		var modifier = new ModelModifier { EnableReasoningMode = true, ReasoningMode = ReasoningMode.Maximum };

		var properties = modifier.ToCompletionProperties().ToList();

		var reasoning = Assert.IsType<ReasoningProperty>(Assert.Single(properties));
		Assert.True(reasoning.Value);
		Assert.Equal(ReasoningEffort.Max, reasoning.Effort);
	}

	[Fact]
	public void ToCompletionProperties_ReasoningDisabled_ReturnsDisabledReasoningProperty()
	{
		var modifier = new ModelModifier { EnableReasoningMode = true, ReasoningMode = ReasoningMode.Disabled };

		var properties = modifier.ToCompletionProperties().ToList();

		var reasoning = Assert.IsType<ReasoningProperty>(Assert.Single(properties));
		Assert.False(reasoning.Value);
	}

	[Fact]
	public void ToCompletionProperties_ReasoningDefault_ReturnsEmptyCollection()
	{
		var modifier = new ModelModifier { EnableReasoningMode = true, ReasoningMode = ReasoningMode.Default };

		var properties = modifier.ToCompletionProperties().ToList();

		Assert.Empty(properties);
	}

	[Fact]
	public void ToCompletionProperties_Temperature_ReturnsTemperatureProperty()
	{
		var modifier = new ModelModifier { EnableTemperature = true, Temperature = 1.0f };

		var properties = modifier.ToCompletionProperties().ToList();

		var temperature = Assert.IsType<TemperatureProperty>(Assert.Single(properties));
		Assert.Equal(0.5f, temperature.Value);
	}

	[Fact]
	public void ToCompletionProperties_MaxTokens_ReturnsMaxTokensProperty()
	{
		var modifier = new ModelModifier { EnableMaxTokens = true, MaxTokens = 4096 };

		var properties = modifier.ToCompletionProperties().ToList();

		var maxTokens = Assert.IsType<MaxTokensProperty>(Assert.Single(properties));
		Assert.Equal(4096, maxTokens.Value);
	}

	[Fact]
	public void ToCompletionProperties_DisabledAdditionalParameter_IsSkipped()
	{
		var modifier = new ModelModifier();
		modifier.AdditionalParameters.Add(new AdditionalGenerationParameter
		{
			Enabled = false,
			ParameterName = "top_p",
			ParameterValue = "0.9"
		});

		var properties = modifier.ToCompletionProperties().ToList();

		Assert.Empty(properties);
	}

	[Fact]
	public void ToCompletionProperties_EnabledAdditionalParameter_ReturnsCustomProperty()
	{
		var modifier = new ModelModifier();
		modifier.AdditionalParameters.Add(new AdditionalGenerationParameter
		{
			Enabled = true,
			ParameterName = "top_p",
			ParameterValue = "0.9"
		});

		var properties = modifier.ToCompletionProperties().ToList();

		var custom = Assert.IsType<CustomProperty>(Assert.Single(properties));
		Assert.Equal("top_p", custom.Name);
	}

	[Fact]
	public void ToCompletionProperties_AllOverrides_ReturnsAllProperties()
	{
		var modifier = new ModelModifier
		{
			EnableReasoningMode = true,
			ReasoningMode = ReasoningMode.High,
			EnableTemperature = true,
			Temperature = 0.8f,
			EnableMaxTokens = true,
			MaxTokens = 2048
		};

		var properties = modifier.ToCompletionProperties().ToList();

		Assert.Equal(3, properties.Count);
	}
}
