using System.Globalization;
using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Elements
{
	[JsonDerived(typeof(ParameterSchemaElement), "textbox")]
	public class ParameterTextBoxElement : ParameterSchemaElement
	{
		/// <summary>
		/// The value type of the textbox: <see cref="ParameterSchemaLimitationType.String"/> or <see cref="ParameterSchemaLimitationType.Number"/>.
		/// </summary>
		public ParameterSchemaLimitationType ValueType
		{
			get;
			set => SetProperty(ref field, value);
		}

		public bool IsMultiline
		{
			get;
			set => SetProperty(ref field, value);
		}

		public string? Default
		{
			get;
			set => SetProperty(ref field, value);
		}

		public string? Placeholder
		{
			get;
			set => SetProperty(ref field, value);
		}

		public override ReactiveNodeValue CreateOrFixValue(ReactiveNodeValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (ValueType == ParameterSchemaLimitationType.Number)
			{
				if (existing is ReactiveNodeNumberValue numberValue)
					return numberValue;

				var final = double.TryParse(Default, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
				log.Append(new ParameterValidationLogEntry
				{
					Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
					OriginalValue = existing?.TakeValueSnapshot(),
					FinalValue = final
				});
				return new ReactiveNodeNumberValue
				{
					Value = final
				};
			}

			if (existing is ReactiveNodeStringValue stringValue && stringValue.Value is not null)
				return stringValue;

			var finalString = Default ?? string.Empty;
			log.Append(new ParameterValidationLogEntry
			{
				Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
				OriginalValue = existing?.TakeValueSnapshot(),
				FinalValue = finalString
			});
			return new ReactiveNodeStringValue
			{
				Value = finalString
			};
		}
	}
}
