using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using LLMDesktopAssistant.Converters;
using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Elements
{
	public abstract class ParameterSchemaElement : NotifyPropertyChanged
	{
		public string? Title
		{
			get;
			set => SetProperty(ref field, value);
		}

		public string? Description
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Creates or validates a value based on the schema element.
		/// If the value is null, it creates a new one.
		/// If not valid - tries to fix value, or creates a new one if fixing is not possible.
		/// </summary>
		/// <param name="existing">The existing value to validate or fix.</param>
		/// <param name="log">A list to log validation or fixing messages.</param>
		/// <returns>The created or fixed value.</returns>
		public abstract ReactiveNodeValue CreateOrFixValue(ReactiveNodeValue? existing, AppendOnlyList<ParameterValidationLogEntry> log);

		/// <summary>
		/// Creates a control for the parameter schema element.
		/// </summary>
		/// <param name="value">The value to use for the control.</param>
		/// <returns>The created control.</returns>
		public abstract Control CreateControl(ReactiveNodeValue value);

		protected BindingBase CreateBinding(object source, string path, BindingMode mode = BindingMode.Default, IValueConverter? converter = null)
		{
			return new ReflectionBinding(path)
			{
				Source = source,
				Mode = mode,
				Converter = converter
			};
		}

		protected Control WrapControl(Control control)
		{
			var title = new TextBlock
			{
				Classes = { ParameterElementsStyles.TitleText },
				[!TextBlock.TextProperty] = CreateBinding(this, nameof(Title)),
				[!TextBlock.IsVisibleProperty] = CreateBinding(this, nameof(Title), converter: StringNonEmptyToBooleanConverter.Instance)
			};
			var description = new TextBlock
			{
				Classes = { ParameterElementsStyles.DescriptionText },
				[!TextBlock.TextProperty] = CreateBinding(this, nameof(Description)),
				[!TextBlock.IsVisibleProperty] = CreateBinding(this, nameof(Description), converter: StringNonEmptyToBooleanConverter.Instance)
			};
			return new StackPanel
			{
				Classes = { ParameterElementsStyles.ParameterContainer },
				Children = { title, description, control }
			};
		}
	}
}
