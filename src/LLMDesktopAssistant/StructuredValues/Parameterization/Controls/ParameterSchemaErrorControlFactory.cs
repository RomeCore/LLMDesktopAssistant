using Avalonia.Controls;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Creates a control for a <see cref="ParameterSchemaErrorElement"/>. Error elements cannot be
	/// edited, so an empty panel is returned.
	/// </summary>
	[Service(typeof(IParameterSchemaControlFactory))]
	public class ParameterSchemaErrorControlFactory : IParameterSchemaControlFactory
	{
		public Type ElementType => typeof(ParameterSchemaErrorElement);

		public Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value)
		{
			return new Panel();
		}
	}
}
