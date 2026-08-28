using Avalonia.Controls;
using LLMDesktopAssistant.Prompting.Parameterization.Values;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.Prompting.Parameterization.Elements
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

		private double ToInteger(double value) => Math.Round(value);

		public override ParameterSchemaValue CreateOrFixValue(ParameterSchemaValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (existing is null)
			{
				var final = ToInteger(Default);
				log.Append(new ParameterValidationLogEntry
				{
					Status = ParameterValidationStatus.Created,
					OriginalValue = null,
					FinalValue = final
				});
				return new ParameterSchemaNumberValue
				{
					Value = final
				};
			}

			if (existing is not ParameterSchemaNumberValue numberValue)
			{
				var final = ToInteger(Default);
				log.Append(new ParameterValidationLogEntry
				{
					Status = ParameterValidationStatus.Invalid,
					OriginalValue = existing.TakeValueSnapshot(),
					FinalValue = final
				});
				return new ParameterSchemaNumberValue
				{
					Value = final
				};
			}

			if (numberValue.Value < Min || numberValue.Value > Max)
			{
				var final = ToInteger(Math.Clamp(numberValue.Value, Min, Max));
				log.Append(new ParameterValidationLogEntry
				{
					Status = ParameterValidationStatus.Fixed,
					OriginalValue = numberValue.Value,
					FinalValue = final
				});
				return new ParameterSchemaNumberValue
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
				return new ParameterSchemaNumberValue
				{
					Value = final
				};
			}

			return existing;
		}

		public override Control CreateControl(ParameterSchemaValue value)
		{
			var numberValue = (ParameterSchemaNumberValue)value;

			var slider = new Slider
			{
				Classes = { ParameterElementsStyles.Slider },
				[!Slider.MinimumProperty] = CreateBinding(this, nameof(Min)),
				[!Slider.MaximumProperty] = CreateBinding(this, nameof(Max)),
				[!Slider.TickFrequencyProperty] = CreateBinding(this, nameof(Step)),
				[!Slider.ValueProperty] = CreateBinding(numberValue, nameof(numberValue.Value))
			};
			if (IsInteger)
			{
				slider.IsSnapToTickEnabled = true;
				slider.TickFrequency = Math.Max(1, Step);
			}
			return WrapControl(slider);
		}
	}
}
