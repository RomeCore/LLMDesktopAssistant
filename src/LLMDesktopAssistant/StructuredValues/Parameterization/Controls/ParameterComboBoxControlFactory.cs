using Avalonia.Controls;
using Avalonia.Data;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Creates a control for a <see cref="ParameterComboBoxElement"/>.
	/// </summary>
	[Service(typeof(IParameterSchemaControlFactory))]
	public class ParameterComboBoxControlFactory : IParameterSchemaControlFactory
	{
		public Type ElementType => typeof(ParameterComboBoxElement);

		public Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value)
		{
			var comboBoxElement = (ParameterComboBoxElement)element;
			if (comboBoxElement.ValueType == ParameterSchemaLimitationType.Boolean)
				return CreateBooleanControl(comboBoxElement, (ReactiveNodeBooleanValue)value);

			return CreateStringControl(comboBoxElement, (ReactiveNodeStringValue)value);
		}

		private static Control CreateStringControl(ParameterComboBoxElement element, ReactiveNodeStringValue stringValue)
		{
			var comboBox = new ComboBox
			{
				Classes = { ParameterElementsStyles.ComboBox },
				ItemsSource = element.Choices,
				IsEditable = element.IsEditable
			};

			if (element.IsEditable)
				comboBox[!ComboBox.TextProperty] = ParameterSchemaControlHelpers.CreateBinding(stringValue, nameof(stringValue.Value), BindingMode.TwoWay);
			else
				comboBox[!ComboBox.SelectedItemProperty] = ParameterSchemaControlHelpers.CreateBinding(stringValue, nameof(stringValue.Value), BindingMode.TwoWay);

			return ParameterSchemaControlHelpers.WrapControl(element, comboBox);
		}

		private static Control CreateBooleanControl(ParameterComboBoxElement element, ReactiveNodeBooleanValue booleanValue)
		{
			var items = new List<ParameterBooleanComboItem>
			{
				new() { Value = true, Title = element.TrueTitle ?? Locale.Get("parameterization.combo.true") },
				new() { Value = false, Title = element.FalseTitle ?? Locale.Get("parameterization.combo.false") }
			};

			var comboBox = new ComboBox
			{
				Classes = { ParameterElementsStyles.ComboBox },
				ItemsSource = items
			};
			comboBox.SelectionChanged += (_, _) =>
			{
				if (comboBox.SelectedItem is ParameterBooleanComboItem item)
					booleanValue.Value = item.Value;
			};
			comboBox.SelectedItem = items.FirstOrDefault(i => i.Value == booleanValue.Value) ?? items[0];

			return ParameterSchemaControlHelpers.WrapControl(element, comboBox);
		}

		private sealed class ParameterBooleanComboItem
		{
			public required bool Value { get; init; }

			public required string Title { get; init; }

			public override string ToString() => Title;
		}
	}
}
