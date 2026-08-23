using System.Text.Json.Nodes;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Completions.Properties;

namespace LLMDesktopAssistant.Providers
{
	/// <summary>
	/// A named container of generation parameters that can be attached to a model via the
	/// model full name: "Provider$Model$Modifier". A <see langword="null"/> value of a
	/// parameter means the modifier does not override it.
	/// </summary>
	public class ModelModifier : NotifyPropertyChanged
	{
		private string _name = string.Empty;
		/// <summary>
		/// Gets or sets the unique name of the modifier. Must not contain '$'.
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string? _hint = null;
		/// <summary>
		/// Gets or sets the human-readable short description (hint) of the modifier.
		/// </summary>
		public string? Hint
		{
			get => _hint;
			set => SetProperty(ref _hint, value);
		}

		private bool _enableReasoningMode = false;
		public bool EnableReasoningMode
		{
			get => _enableReasoningMode;
			set => SetProperty(ref _enableReasoningMode, value);
		}

		private ReasoningMode _reasoningMode = ReasoningMode.Disabled;
		/// <summary>
		/// Gets or sets the reasoning settings override.
		/// </summary>
		public ReasoningMode ReasoningMode
		{
			get => _reasoningMode;
			set => SetProperty(ref _reasoningMode, value);
		}

		private bool _enableTemperature = false;
		public bool EnableTemperature
		{
			get => _enableTemperature;
			set => SetProperty(ref _enableTemperature, value);
		}

		private float _temperature = 1.0f;
		/// <summary>
		/// Gets or sets the temperature override in range from 0 to 2.
		/// </summary>
		public float Temperature
		{
			get => _temperature;
			set => SetProperty(ref _temperature, value);
		}

		private bool _enableMaxTokens = false;
		public bool EnableMaxTokens
		{
			get => _enableMaxTokens;
			set => SetProperty(ref _enableMaxTokens, value);
		}

		private int _maxTokens = 8096;
		/// <summary>
		/// Gets or sets the maximum number of tokens to generate.
		/// </summary>
		public int MaxTokens
		{
			get => _maxTokens;
			set => SetProperty(ref _maxTokens, value);
		}

		private readonly RangeObservableCollection<AdditionalGenerationParameter> _additionalParameters = [];
		/// <summary>
		/// Gets or sets the additional parameters in a key-value format passed to the API.
		/// </summary>
		public RangeObservableCollection<AdditionalGenerationParameter> AdditionalParameters
		{
			get => _additionalParameters;
			set => _additionalParameters.Reset(value);
		}

		/// <summary>
		/// Converts the modifier to RCLLM completion properties.
		/// </summary>
		/// <returns>A collection of completion properties representing the modifier overrides.</returns>
		public IEnumerable<CompletionProperty> ToCompletionProperties()
		{
			var result = new List<CompletionProperty>();

			if (EnableReasoningMode)
			{
				if (ReasoningMode == ReasoningMode.Disabled)
					result.Add(new ReasoningProperty(false));
				else if (ReasoningMode != ReasoningMode.Default)
					result.Add(new ReasoningProperty(ReasoningMode switch
					{
						ReasoningMode.None => ReasoningEffort.None,
						ReasoningMode.Minimal => ReasoningEffort.Minimal,
						ReasoningMode.Low => ReasoningEffort.Low,
						ReasoningMode.Medium => ReasoningEffort.Medium,
						ReasoningMode.High => ReasoningEffort.High,
						ReasoningMode.XHigh => ReasoningEffort.XHigh,
						ReasoningMode.Maximum => ReasoningEffort.Max,
						_ => ReasoningEffort.Medium
					}));
			}

			if (EnableTemperature)
				result.Add(new TemperatureProperty(Temperature / 2.0f));

			if (EnableMaxTokens)
				result.Add(new MaxTokensProperty(MaxTokens));

			foreach (var parameter in AdditionalParameters)
			{
				if (!parameter.Enabled)
					continue;

				result.Add(new CustomProperty(parameter.ParameterName, JsonNode.Parse(parameter.ParameterValue)!));
			}

			return result;
		}
	}
}
