using Avalonia.Controls;
using Avalonia.Data;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Creates a control for a <see cref="ParameterSliderElement"/>.
	/// </summary>
	[Service(typeof(IParameterSchemaControlFactory))]
	public class ParameterSliderControlFactory : IParameterSchemaControlFactory
	{
		public Type ElementType => typeof(ParameterSliderElement);

		public Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value)
		{
			var sliderElement = (ParameterSliderElement)element;
			var numberValue = (ReactiveNodeNumberValue)value;

			var slider = new Slider
			{
				Classes = { ParameterElementsStyles.Slider },
				[!Slider.MinimumProperty] = ParameterSchemaControlHelpers.CreateBinding(sliderElement, nameof(ParameterSliderElement.Min)),
				[!Slider.MaximumProperty] = ParameterSchemaControlHelpers.CreateBinding(sliderElement, nameof(ParameterSliderElement.Max)),
				[!Slider.TickFrequencyProperty] = ParameterSchemaControlHelpers.CreateBinding(sliderElement, nameof(ParameterSliderElement.Step)),
				[!Slider.ValueProperty] = ParameterSchemaControlHelpers.CreateBinding(numberValue, nameof(numberValue.Value))
			};
			if (sliderElement.IsInteger || Math.Abs(sliderElement.Step) > double.Epsilon)
			{
				slider.IsSnapToTickEnabled = true;
				if (sliderElement.IsInteger)
					slider.TickFrequency = Math.Max(1, sliderElement.Step);
				else
					slider.TickFrequency = sliderElement.Step;
			}

			var valueText = new TextBlock
			{
				Classes = { ParameterElementsStyles.SliderValueText },
				[!TextBlock.TextProperty] = ParameterSchemaControlHelpers.CreateBinding(numberValue, nameof(numberValue.Value), BindingMode.OneWay)
			};

			var panel = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(new GridLength(50))
				},
				Children = { slider, valueText }
			};
			Grid.SetColumn(valueText, 1);

			return ParameterSchemaControlHelpers.WrapControl(element, panel);
		}
	}
}
