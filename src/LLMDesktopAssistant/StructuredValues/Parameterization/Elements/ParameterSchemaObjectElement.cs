using Avalonia.Controls;
using LLMDesktopAssistant.StructuredValues.Parameterization.MVVM;
using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Elements
{
	[JsonDerived(typeof(ParameterSchemaElement), "object")]
	public class ParameterSchemaObjectElement : ParameterSchemaElement
	{
		private readonly ObservableDictionary<string, ParameterSchemaElement> _properties = [];
		public ObservableDictionary<string, ParameterSchemaElement> Properties
		{
			get => _properties;
			set => _properties.Reset(value);
		}

		public override ReactiveNodeValue CreateOrFixValue(ReactiveNodeValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (existing is ReactiveNodeDictionaryValue dictValue)
			{
				foreach (var (key, element) in Properties)
				{
					var propertyValue = dictValue.Items.TryGetValue(key, out var existingValue)
						? element.CreateOrFixValue(existingValue, log)
						: element.CreateOrFixValue(null, log);
					dictValue.Items[key] = propertyValue;
				}
				var removedKeys = dictValue.Items.Keys.Except(Properties.Keys).ToList();
				foreach (var key in removedKeys)
					dictValue.Items.Remove(key);
				return dictValue;
			}

			log.Append(new ParameterValidationLogEntry
			{
				Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
				OriginalValue = existing?.TakeValueSnapshot(),
				FinalValue = null
			});

			var newDict = new ReactiveNodeDictionaryValue();
			foreach (var (key, element) in Properties)
				newDict.Items.Add(key, element.CreateOrFixValue(null, log));
			return newDict;
		}

		public override Control CreateControl(ReactiveNodeValue value)
		{
			var dictValue = (ReactiveNodeDictionaryValue)value;
			var viewModel = new ParameterSchemaObjectViewModel(this, dictValue);
			return WrapControl(new ContentControl
			{
				Content = viewModel
			});
		}
	}
}
