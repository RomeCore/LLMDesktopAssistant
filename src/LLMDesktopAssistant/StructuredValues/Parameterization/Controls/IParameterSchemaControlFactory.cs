using Avalonia.Controls;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Creates Avalonia controls for a specific type of <see cref="ParameterSchemaElement"/>.
	/// </summary>
	public interface IParameterSchemaControlFactory
	{
		/// <summary>
		/// The type of the schema element this factory creates controls for.
		/// </summary>
		Type ElementType { get; }

		/// <summary>
		/// Creates a control that edits the given value according to the schema element.
		/// </summary>
		/// <param name="element">The schema element to create a control for.</param>
		/// <param name="value">The value to edit with the control.</param>
		/// <returns>The created control.</returns>
		Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value);
	}
}
