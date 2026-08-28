using Avalonia.Controls;
using LLMDesktopAssistant.Prompting.Parameterization.Values;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.Prompting.Parameterization.Elements
{
	[JsonDerived(typeof(ParameterSchemaElement), "checkbox")]
	public class ParameterCheckboxElement : ParameterSchemaElement
	{
		public bool Default
		{
			get;
			set => SetProperty(ref field, value);
		}

		public override ParameterSchemaValue CreateOrFixValue(ParameterSchemaValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (existing is ParameterSchemaBooleanValue booleanValue)
				return booleanValue;

			log.Append(new ParameterValidationLogEntry
			{
				Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
				OriginalValue = existing?.TakeValueSnapshot(),
				FinalValue = Default
			});
			return new ParameterSchemaBooleanValue
			{
				Value = Default
			};
		}

		public override Control CreateControl(ParameterSchemaValue value)
		{
			var booleanValue = (ParameterSchemaBooleanValue)value;

			var checkBox = new CheckBox
			{
				Classes = { ParameterElementsStyles.Checkbox },
				[!CheckBox.IsCheckedProperty] = CreateBinding(booleanValue, nameof(booleanValue.Value))
			};
			return WrapControl(checkBox);
		}
	}
}
