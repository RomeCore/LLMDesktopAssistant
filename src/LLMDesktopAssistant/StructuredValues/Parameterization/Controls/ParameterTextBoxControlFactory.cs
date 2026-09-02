using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Creates a control for a <see cref="ParameterTextBoxElement"/>.
	/// </summary>
	[Service(typeof(IParameterSchemaControlFactory))]
	public class ParameterTextBoxControlFactory : IParameterSchemaControlFactory
	{
		public Type ElementType => typeof(ParameterTextBoxElement);

		public Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value)
		{
			var textBoxElement = (ParameterTextBoxElement)element;
			if (textBoxElement.ValueType == ParameterSchemaLimitationType.Number)
				return CreateNumberControl(textBoxElement, (ReactiveNodeNumberValue)value);

			return CreateStringControl(textBoxElement, (ReactiveNodeStringValue)value);
		}

		private static Control CreateStringControl(ParameterTextBoxElement element, ReactiveNodeStringValue stringValue)
		{
			var textBox = new TextBox
			{
				Classes = { ParameterElementsStyles.TextBox },
				PlaceholderText = element.Placeholder,
				[!TextBox.TextProperty] = ParameterSchemaControlHelpers.CreateBinding(stringValue, nameof(stringValue.Value), BindingMode.TwoWay)
			};
			ApplyMultiline(element, textBox);
			return ParameterSchemaControlHelpers.WrapControl(element, textBox);
		}

		private static Control CreateNumberControl(ParameterTextBoxElement element, ReactiveNodeNumberValue numberValue)
		{
			var textBox = new TextBox
			{
				Classes = { ParameterElementsStyles.TextBox },
				PlaceholderText = element.Placeholder,
				Text = numberValue.Value.ToString(CultureInfo.InvariantCulture)
			};
			ApplyMultiline(element, textBox);

			bool updating = false;
			textBox.TextChanged += (_, _) =>
			{
				if (updating)
					return;
				if (double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
				{
					updating = true;
					numberValue.Value = parsed;
					updating = false;
				}
			};
			numberValue.SubscribeChanged(nameof(ReactiveNodeNumberValue.Value), (object? _) =>
			{
				updating = true;
				textBox.Text = numberValue.Value.ToString(CultureInfo.InvariantCulture);
				updating = false;
			});

			return ParameterSchemaControlHelpers.WrapControl(element, textBox);
		}

		private static void ApplyMultiline(ParameterTextBoxElement element, TextBox textBox)
		{
			if (!element.IsMultiline)
				return;
			textBox.AcceptsReturn = true;
			textBox.TextWrapping = TextWrapping.Wrap;
			textBox.MinLines = 3;
			textBox.Classes.Add(ParameterElementsStyles.TextBoxMultiline);
		}
	}
}
