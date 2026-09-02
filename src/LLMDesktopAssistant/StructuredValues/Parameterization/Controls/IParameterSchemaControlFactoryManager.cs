using Avalonia.Controls;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Creates controls for parameter schema elements by resolving the registered
	/// <see cref="IParameterSchemaControlFactory"/> for the element type.
	/// </summary>
	public interface IParameterSchemaControlFactoryManager
	{
		/// <summary>
		/// Creates a control that edits the given value according to the schema element.
		/// </summary>
		/// <param name="element">The schema element to create a control for.</param>
		/// <param name="value">The value to edit with the control.</param>
		/// <returns>The created control.</returns>
		Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value);
	}
}
