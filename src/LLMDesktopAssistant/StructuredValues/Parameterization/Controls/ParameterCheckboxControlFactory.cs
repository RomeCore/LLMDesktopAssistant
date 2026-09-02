using Avalonia.Controls;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Creates a control for a <see cref="ParameterCheckboxElement"/>.
	/// </summary>
	[Service(typeof(IParameterSchemaControlFactory))]
	public class ParameterCheckboxControlFactory : IParameterSchemaControlFactory
	{
		public Type ElementType => typeof(ParameterCheckboxElement);

		public Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value)
		{
			var booleanValue = (ReactiveNodeBooleanValue)value;

			var checkBox = new CheckBox
			{
				Classes = { ParameterElementsStyles.Checkbox },
				[!CheckBox.IsCheckedProperty] = ParameterSchemaControlHelpers.CreateBinding(booleanValue, nameof(booleanValue.Value))
			};
			return ParameterSchemaControlHelpers.WrapControl(element, checkBox);
		}
	}
}
