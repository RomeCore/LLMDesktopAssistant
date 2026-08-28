using Avalonia.Controls;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Prompting.Parameterization.Elements;
using LLMDesktopAssistant.Prompting.Parameterization.Values;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting.Parameterization.MVVM
{
	/// <summary>
	/// ViewModel for rendering an object parameter schema element and its values.
	/// </summary>
	[ViewModelFor(typeof(ParameterSchemaObjectView))]
	public class ParameterSchemaObjectViewModel : ViewModelBase
	{
		private readonly ParameterSchemaObjectElement _element;
		private readonly ParameterSchemaDictionaryValue _value;

		public RangeObservableCollection<ParameterSchemaObjectItemViewModel> Items { get; } = [];

		public ParameterSchemaObjectViewModel(ParameterSchemaObjectElement element, ParameterSchemaDictionaryValue value)
		{
			_element = element;
			_value = value;

			foreach (var (key, propertyElement) in element.Properties)
			{
				var propertyValue = propertyElement.CreateOrFixValue(
					value.Items.TryGetValue(key, out var existing) ? existing : null, []);
				value.Items[key] = propertyValue;
				Items.Add(new ParameterSchemaObjectItemViewModel(key, propertyElement, propertyValue));
			}
		}
	}

	/// <summary>
	/// A single property of an object parameter schema element.
	/// </summary>
	public class ParameterSchemaObjectItemViewModel
	{
		public string Key { get; }

		public ParameterSchemaElement Element { get; }

		public ParameterSchemaValue Value { get; }

		public Control Control { get; }

		public ParameterSchemaObjectItemViewModel(string key, ParameterSchemaElement element, ParameterSchemaValue value)
		{
			Key = key;
			Element = element;
			Value = value;
			Control = element.CreateControl(value);
		}
	}
}
