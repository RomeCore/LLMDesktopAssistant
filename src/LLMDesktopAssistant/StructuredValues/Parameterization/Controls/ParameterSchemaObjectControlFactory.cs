using Avalonia.Controls;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Parameterization.MVVM;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Creates a control for a <see cref="ParameterSchemaObjectElement"/>.
	/// </summary>
	[Service(typeof(IParameterSchemaControlFactory))]
	public class ParameterSchemaObjectControlFactory(
		IParameterSchemaControlFactoryManager controlFactory
	) : IParameterSchemaControlFactory
	{
		public Type ElementType => typeof(ParameterSchemaObjectElement);

		public Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value)
		{
			var objectElement = (ParameterSchemaObjectElement)element;
			var dictValue = (ReactiveNodeDictionaryValue)value;
			var viewModel = new ParameterSchemaObjectViewModel(objectElement, dictValue, controlFactory);
			return ParameterSchemaControlHelpers.WrapControl(element, new ContentControl
			{
				Content = viewModel
			});
		}
	}
}
