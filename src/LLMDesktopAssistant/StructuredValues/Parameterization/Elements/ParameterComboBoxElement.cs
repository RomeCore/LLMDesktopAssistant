using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Elements
{
	[JsonDerived(typeof(ParameterSchemaElement), "combobox")]
	public class ParameterComboBoxElement : ParameterSchemaElement
	{
		/// <summary>
		/// The value type of the combobox: <see cref="ParameterSchemaLimitationType.String"/> or <see cref="ParameterSchemaLimitationType.Boolean"/>.
		/// </summary>
		public ParameterSchemaLimitationType ValueType
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The list of choices for a string combobox.
		/// </summary>
		public IReadOnlyList<string>? Choices
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Whether the user can type a custom value for a string combobox.
		/// </summary>
		public bool IsEditable
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The default value for a string combobox.
		/// </summary>
		public string? Default
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The default value for a boolean combobox.
		/// </summary>
		public bool DefaultBoolean
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The title for the <c>true</c> option of a boolean combobox.
		/// </summary>
		public string? TrueTitle
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The title for the <c>false</c> option of a boolean combobox.
		/// </summary>
		public string? FalseTitle
		{
			get;
			set => SetProperty(ref field, value);
		}

		public override ReactiveNodeValue CreateOrFixValue(ReactiveNodeValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (ValueType == ParameterSchemaLimitationType.Boolean)
			{
				if (existing is ReactiveNodeBooleanValue booleanValue)
					return booleanValue;

				log.Append(new ParameterValidationLogEntry
				{
					Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
					OriginalValue = existing?.TakeValueSnapshot(),
					FinalValue = DefaultBoolean
				});
				return new ReactiveNodeBooleanValue
				{
					Value = DefaultBoolean
				};
			}

			if (existing is ReactiveNodeStringValue stringValue &&
				stringValue.Value is not null &&
				(IsEditable || Choices is null || Choices.Contains(stringValue.Value)))
			{
				return stringValue;
			}

			var final = existing is ReactiveNodeStringValue existingString && existingString.Value is not null
				? existingString.Value
				: Default ?? Choices?.FirstOrDefault() ?? string.Empty;

			log.Append(new ParameterValidationLogEntry
			{
				Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
				OriginalValue = existing?.TakeValueSnapshot(),
				FinalValue = final
			});
			return new ReactiveNodeStringValue
			{
				Value = final
			};
		}
	}
}
