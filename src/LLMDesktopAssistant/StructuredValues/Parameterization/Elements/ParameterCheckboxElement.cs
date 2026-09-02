using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Elements
{
	[JsonDerived(typeof(ParameterSchemaElement), "checkbox")]
	public class ParameterCheckboxElement : ParameterSchemaElement
	{
		public bool Default
		{
			get;
			set => SetProperty(ref field, value);
		}

		public override ReactiveNodeValue CreateOrFixValue(ReactiveNodeValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (existing is ReactiveNodeBooleanValue booleanValue)
				return booleanValue;

			log.Append(new ParameterValidationLogEntry
			{
				Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
				OriginalValue = existing?.TakeValueSnapshot(),
				FinalValue = Default
			});
			return new ReactiveNodeBooleanValue
			{
				Value = Default
			};
		}
	}
}
