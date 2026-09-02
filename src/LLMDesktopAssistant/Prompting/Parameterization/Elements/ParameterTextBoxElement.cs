using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using LLMDesktopAssistant.StructuredValues.Reactive;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.Prompting.Parameterization.Elements
{
	[JsonDerived(typeof(ParameterSchemaElement), "textbox")]
	public class ParameterTextBoxElement : ParameterSchemaElement
	{
		/// <summary>
		/// The value type of the textbox: <see cref="ParameterSchemaLimitationType.String"/> or <see cref="ParameterSchemaLimitationType.Number"/>.
		/// </summary>
		public ParameterSchemaLimitationType ValueType
		{
			get;
			set => SetProperty(ref field, value);
		}

		public bool IsMultiline
		{
			get;
			set => SetProperty(ref field, value);
		}

		public string? Default
		{
			get;
			set => SetProperty(ref field, value);
		}

		public string? Placeholder
		{
			get;
			set => SetProperty(ref field, value);
		}

		public override ReactiveNodeValue CreateOrFixValue(ReactiveNodeValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (ValueType == ParameterSchemaLimitationType.Number)
			{
				if (existing is ReactiveNodeNumberValue numberValue)
					return numberValue;

				var final = double.TryParse(Default, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
				log.Append(new ParameterValidationLogEntry
				{
					Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
					OriginalValue = existing?.TakeValueSnapshot(),
					FinalValue = final
				});
				return new ReactiveNodeNumberValue
				{
					Value = final
				};
			}

			if (existing is ReactiveNodeStringValue stringValue && stringValue.Value is not null)
				return stringValue;

			var finalString = Default ?? string.Empty;
			log.Append(new ParameterValidationLogEntry
			{
				Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
				OriginalValue = existing?.TakeValueSnapshot(),
				FinalValue = finalString
			});
			return new ReactiveNodeStringValue
			{
				Value = finalString
			};
		}

		public override Control CreateControl(ReactiveNodeValue value)
		{
			if (ValueType == ParameterSchemaLimitationType.Number)
				return CreateNumberControl((ReactiveNodeNumberValue)value);

			return CreateStringControl((ReactiveNodeStringValue)value);
		}

		private Control CreateStringControl(ReactiveNodeStringValue stringValue)
		{
			var textBox = new TextBox
			{
				Classes = { ParameterElementsStyles.TextBox },
				PlaceholderText = Placeholder,
				[!TextBox.TextProperty] = CreateBinding(stringValue, nameof(stringValue.Value), BindingMode.TwoWay)
			};
			ApplyMultiline(textBox);
			return WrapControl(textBox);
		}

		private Control CreateNumberControl(ReactiveNodeNumberValue numberValue)
		{
			var textBox = new TextBox
			{
				Classes = { ParameterElementsStyles.TextBox },
				PlaceholderText = Placeholder,
				Text = numberValue.Value.ToString(CultureInfo.InvariantCulture)
			};
			ApplyMultiline(textBox);

			bool updating = false;
			textBox.TextChanged += (_, _) =>
			{
				if (updating)
					return;
				if (double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
				{
					updating = true;
					numberValue.Value = parsed;
					updating = false;
				}
			};
			numberValue.SubscribeChanged(nameof(ReactiveNodeNumberValue.Value), (object? _) =>
			{
				updating = true;
				textBox.Text = numberValue.Value.ToString(CultureInfo.InvariantCulture);
				updating = false;
			});

			return WrapControl(textBox);
		}

		private void ApplyMultiline(TextBox textBox)
		{
			if (!IsMultiline)
				return;
			textBox.AcceptsReturn = true;
			textBox.TextWrapping = TextWrapping.Wrap;
			textBox.MinLines = 3;
			textBox.Classes.Add(ParameterElementsStyles.TextBoxMultiline);
		}
	}
}
