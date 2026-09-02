using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using LLMDesktopAssistant.Converters;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	/// <summary>
	/// Helper methods for building parameter schema controls.
	/// </summary>
	public static class ParameterSchemaControlHelpers
	{
		public static BindingBase CreateBinding(object source, string path, BindingMode mode = BindingMode.Default, IValueConverter? converter = null)
		{
			return new ReflectionBinding(path)
			{
				Source = source,
				Mode = mode,
				Converter = converter
			};
		}

		/// <summary>
		/// Wraps the given control into a container that shows the element's title and description.
		/// </summary>
		public static Control WrapControl(ParameterSchemaElement element, Control control)
		{
			var title = new TextBlock
			{
				Classes = { ParameterElementsStyles.TitleText },
				[!TextBlock.TextProperty] = CreateBinding(element, nameof(ParameterSchemaElement.Title)),
				[!TextBlock.IsVisibleProperty] = CreateBinding(element, nameof(ParameterSchemaElement.Title), converter: StringNonEmptyToBooleanConverter.Instance)
			};
			var description = new TextBlock
			{
				Classes = { ParameterElementsStyles.DescriptionText },
				[!TextBlock.TextProperty] = CreateBinding(element, nameof(ParameterSchemaElement.Description)),
				[!TextBlock.IsVisibleProperty] = CreateBinding(element, nameof(ParameterSchemaElement.Description), converter: StringNonEmptyToBooleanConverter.Instance)
			};
			return new StackPanel
			{
				Classes = { ParameterElementsStyles.ParameterContainer },
				Children = { title, description, control }
			};
		}
	}
}
