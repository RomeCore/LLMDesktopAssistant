using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Prompting.Parameterization;
using LLMDesktopAssistant.Prompting.Parameterization.Values;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.MVVM.Debug;

/// <summary>
/// View model for the parameterization debug page: parses a <c>params_schema</c> LLT metadata
/// block, builds the parameter schema and shows the generated parameterization control together
/// with the current value snapshot in JSON.
/// </summary>
[ViewModelFor(typeof(ParameterizationDebugPageView))]
public class ParameterizationDebugPageViewModel : ViewModelBase
{
	private const string DefaultSample = """
		@template parameterization_demo
		{
			@metadata
			{
				guid: 'aaaaaaaa-0000-4000-a000-0000000000ff',
				lang: 'en-US',
				type: 'text',
				title: 'Parameterization demo',
				params_schema:
				{
					formatting:
					{
						type: 'textbox/string',
						title: 'Formatting',
						description: 'Custom formatting instructions',
						isMultiline: true,
						default: 'Use bullet points',
						placeholder: 'Type formatting instructions...'
					},
					max_repetitions:
					{
						type: 'textbox/number',
						title: 'Max repetitions',
						default: 3
					},
					enable_emojis:
					{
						type: 'checkbox',
						title: 'Enable emojis',
						description: 'Allow emojis in the response',
						default: true
					},
					temperature:
					{
						type: 'slider/number',
						title: 'Temperature',
						description: 'Creativity temperature',
						min: 0,
						max: 2,
						step: 0.1,
						default: 0.7
					},
					top_p:
					{
						type: 'slider/integer',
						title: 'Top P (percent)',
						min: 0,
						max: 100,
						default: 80
					},
					style:
					{
						type: 'combobox/string',
						title: 'Style',
						choices: [ 'Formal', 'Casual', 'Technical', 'Poetic' ],
						default: 'Casual'
					},
					detail_level:
					{
						type: 'combobox/boolean',
						title: 'Detail level',
						trueTitle: 'Detailed',
						falseTitle: 'Brief',
						default: true
					},
					keywords:
					{
						type: 'list',
						title: 'Keywords',
						description: 'Add or remove keywords (1-5)',
						min: 1,
						max: 5,
						items:
						{
							type: 'textbox/string',
							default: 'keyword'
						}
					},
					limits:
					{
						type: 'object',
						title: 'Limits',
						properties:
						{
							max_length:
							{
								type: 'slider/integer',
								title: 'Max length',
								min: 100,
								max: 4000,
								step: 100,
								default: 1000
							},
							allow_links:
							{
								type: 'checkbox',
								title: 'Allow links',
								default: false
							}
						}
					}
				}
			}
			This is a demo template with parameters.
		}
		""";

	private string _text = DefaultSample;
	/// <summary>
	/// Gets or sets the LLT template text with a <c>params_schema</c> metadata block.
	/// </summary>
	public string Text
	{
		get => _text;
		set => SetProperty(ref _text, value);
	}

	private Control? _parameterControl;
	/// <summary>
	/// Gets the generated parameterization control.
	/// </summary>
	public Control? ParameterControl
	{
		get => _parameterControl;
		private set => SetProperty(ref _parameterControl, value);
	}

	private string? _valueJson;
	/// <summary>
	/// Gets the JSON snapshot of the current parameter values.
	/// </summary>
	public string? ValueJson
	{
		get => _valueJson;
		private set => SetProperty(ref _valueJson, value);
	}

	private string? _parseErrorsText;
	/// <summary>
	/// Gets the schema parsing errors, if any.
	/// </summary>
	public string? ParseErrorsText
	{
		get => _parseErrorsText;
		private set => SetProperty(ref _parseErrorsText, value);
	}

	private string? _validationLogText;
	/// <summary>
	/// Gets the value validation/creation log.
	/// </summary>
	public string? ValidationLogText
	{
		get => _validationLogText;
		private set => SetProperty(ref _validationLogText, value);
	}

	private ParameterSchema? _schema;
	private ParameterSchemaValue? _rootValue;

	/// <summary>
	/// Gets the command that parses the current text and rebuilds the parameterization control.
	/// </summary>
	public IRelayCommand ParseCommand { get; }

	/// <summary>
	/// Gets the command that restores the default sample text and re-parses it.
	/// </summary>
	public IRelayCommand ResetCommand { get; }

	/// <summary>
	/// Gets the command that refreshes the JSON value snapshot from the current parameter values.
	/// </summary>
	public IRelayCommand RefreshValueCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ParameterizationDebugPageViewModel"/> class.
	/// </summary>
	public ParameterizationDebugPageViewModel()
	{
		ParseCommand = new RelayCommand(Parse);
		ResetCommand = new RelayCommand(Reset);
		RefreshValueCommand = new RelayCommand(RefreshValue, () => _rootValue is not null);
		Parse();
	}

	private void Reset()
	{
		Text = DefaultSample;
		Parse();
	}

	private void Parse()
	{
		ParameterControl = null;
		ValueJson = null;
		ParseErrorsText = null;
		ValidationLogText = null;
		_schema = null;
		_rootValue = null;
		RefreshValueCommand.NotifyCanExecuteChanged();

		try
		{
			var parser = new LLTParser();
			var template = parser.Parse(Text, [new ParameterSchemaTemplateMetadataFactory()]).FirstOrDefault();
			var schemaMetadata = template?.Metadata.TryGet<ParameterSchemaTemplateMetadata>();
			if (schemaMetadata is null)
			{
				ParseErrorsText = "No 'params_schema' metadata found in the template.";
				return;
			}

			var parserManager = ServiceRegistry.Provider.GetService<IParameterSchemaParserManager>();
			if (parserManager is null)
			{
				ParseErrorsText = "IParameterSchemaParserManager service is not registered.";
				return;
			}

			var errors = new AppendOnlyList<ParameterSchemaParsingError>();
			_schema = parserManager.ParseRoot(schemaMetadata.Value, errors);
			if (errors.Count > 0)
			{
				ParseErrorsText = string.Join(Environment.NewLine,
					errors.Select(e => $"[{e.Path.Value}] {e.Type}: {e.Message}"));
			}

			var log = new AppendOnlyList<ParameterValidationLogEntry>();
			_rootValue = _schema.Root.CreateOrFixValue(null, log);
			ValidationLogText = log.Count > 0
				? string.Join(Environment.NewLine,
					log.Select(l => $"{l.Status}: {l.OriginalValue?.ToString() ?? "null"} -> {l.FinalValue?.ToString() ?? "null"}"))
				: "(empty)";

			ParameterControl = _schema.Root.CreateControl(_rootValue);
			RefreshValue();
			RefreshValueCommand.NotifyCanExecuteChanged();
		}
		catch (Exception ex)
		{
			ParseErrorsText = ex.ToString();
		}
	}

	private void RefreshValue()
	{
		if (_rootValue is null)
			return;
		var node = ToJsonNode(_rootValue);
		ValueJson = node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null";
	}

	private static JsonNode? ToJsonNode(ParameterSchemaValue value) => value switch
	{
		ParameterSchemaNullValue => null,
		ParameterSchemaBooleanValue b => JsonValue.Create(b.Value),
		ParameterSchemaNumberValue n => JsonValue.Create(n.Value),
		ParameterSchemaStringValue s => JsonValue.Create(s.Value),
		ParameterSchemaArrayValue a => new JsonArray(a.Items.Select(ToJsonNode).ToArray()),
		ParameterSchemaDictionaryValue d => new JsonObject(d.Items.Select(kvp => KeyValuePair.Create(kvp.Key, ToJsonNode(kvp.Value)))),
		_ => null
	};
}
