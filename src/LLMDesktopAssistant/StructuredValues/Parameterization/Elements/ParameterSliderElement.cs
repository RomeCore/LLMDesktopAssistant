using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Elements
{
	[JsonDerived(typeof(ParameterSchemaElement), "slider")]
	public class ParameterSliderElement : ParameterSchemaElement
	{
		/// <summary>
		/// Whether the slider operates with integer values only.
		/// </summary>
		public bool IsInteger
		{
			get;
			set => SetProperty(ref field, value);
		}

		public double Min
		{
			get;
			set => SetProperty(ref field, value);
		}

		public double Max
		{
			get;
			set => SetProperty(ref field, value);
		}

		public double Default
		{
			get;
			set => SetProperty(ref field, value);
		}

		public double Step
		{
			get;
			set => SetProperty(ref field, value);
		}

		private double TryToInteger(double value)
		{
			if (IsInteger)
				return Math.Round(value);
			return value;
		}

		public override ReactiveNodeValue CreateOrFixValue(ReactiveNodeValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (existing is null)
			{
				var final = TryToInteger(Default);
				log.Append(new ParameterValidationLogEntry
				{
					Status = ParameterValidationStatus.Created,
					OriginalValue = null,
					FinalValue = final
				});
				return new ReactiveNodeNumberValue
				{
					Value = final
				};
			}

			if (existing is not ReactiveNodeNumberValue numberValue)
			{
				var final = TryToInteger(Default);
				log.Append(new ParameterValidationLogEntry
				{
					Status = ParameterValidationStatus.Invalid,
					OriginalValue = existing.TakeValueSnapshot(),
					FinalValue = final
				});
				return new ReactiveNodeNumberValue
				{
					Value = final
				};
			}

			if (numberValue.Value < Min || numberValue.Value > Max)
			{
				var final = TryToInteger(Math.Clamp(numberValue.Value, Min, Max));
				log.Append(new ParameterValidationLogEntry
				{
					Status = ParameterValidationStatus.Fixed,
					OriginalValue = numberValue.Value,
					FinalValue = final
				});
				return new ReactiveNodeNumberValue
				{
					Value = final
				};
			}

			if (IsInteger && numberValue.Value != Math.Round(numberValue.Value))
			{
				var final = Math.Round(numberValue.Value);
				log.Append(new ParameterValidationLogEntry
				{
					Status = ParameterValidationStatus.Fixed,
					OriginalValue = numberValue.Value,
					FinalValue = final
				});
				return new ReactiveNodeNumberValue
				{
					Value = final
				};
			}

			return existing;
		}
	}
}
