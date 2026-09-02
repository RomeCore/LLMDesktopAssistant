using Avalonia.Controls;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Parameterization.MVVM;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Creates a control for a <see cref="ParameterListElement"/>.
	/// </summary>
	[Service(typeof(IParameterSchemaControlFactory))]
	public class ParameterListControlFactory(
		IParameterSchemaControlFactoryManager controlFactory
	) : IParameterSchemaControlFactory
	{
		public Type ElementType => typeof(ParameterListElement);

		public Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value)
		{
			var listElement = (ParameterListElement)element;
			var arrayValue = (ReactiveNodeArrayValue)value;
			var viewModel = new ParameterSchemaListViewModel(listElement, arrayValue, controlFactory);
			return ParameterSchemaControlHelpers.WrapControl(element, new ContentControl
			{
				Content = viewModel
			});
		}
	}
}
