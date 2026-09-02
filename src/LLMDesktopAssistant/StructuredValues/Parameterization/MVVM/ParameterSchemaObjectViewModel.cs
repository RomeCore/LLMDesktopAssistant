using Avalonia.Controls;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.MVVM
{
	/// <summary>
	/// ViewModel for rendering an object parameter schema element and its values.
	/// </summary>
	[ViewModelFor(typeof(ParameterSchemaObjectView))]
	public class ParameterSchemaObjectViewModel : ViewModelBase
	{
		private readonly ParameterSchemaObjectElement _element;
		private readonly ReactiveNodeDictionaryValue _value;

		public RangeObservableCollection<ParameterSchemaObjectItemViewModel> Items { get; } = [];

		public ParameterSchemaObjectViewModel(ParameterSchemaObjectElement element, ReactiveNodeDictionaryValue value)
		{
			_element = element;
			_value = value;

			foreach (var (key, propertyElement) in element.Properties)
			{
				var propertyValue = propertyElement.CreateOrFixValue(
					value.Items.TryGetValue(key, out var existing) ? existing : null, []);
				value.Items[key] = propertyValue;
				Items.Add(new ParameterSchemaObjectItemViewModel(propertyElement, propertyValue));
			}
		}
	}

	/// <summary>
	/// A single property of an object parameter schema element.
	/// </summary>
	public class ParameterSchemaObjectItemViewModel
	{
		public ParameterSchemaElement Element { get; }

		public ReactiveNodeValue Value { get; }

		public Control Control { get; }

		public ParameterSchemaObjectItemViewModel(ParameterSchemaElement element, ReactiveNodeValue value)
		{
			Element = element;
			Value = value;
			Control = element.CreateControl(value);
		}
	}
}
