using Avalonia.Controls;
using Avalonia.Data;
using LLMDesktopAssistant.Prompting.Parameterization.Values;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.Prompting.Parameterization.Elements
{
	[JsonDerived(typeof(ParameterSchemaElement), "combobox")]
	public class ParameterComboBoxElement : ParameterSchemaElement
	{
		/// <summary>
		/// The value type of the combobox: <see cref="ParameterSchemaLimitationType.String"/> or <see cref="ParameterSchemaLimitationType.Boolean"/>.
		/// </summary>
		public ParameterSchemaLimitationType ValueType
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The list of choices for a string combobox.
		/// </summary>
		public IReadOnlyList<string>? Choices
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Whether the user can type a custom value for a string combobox.
		/// </summary>
		public bool IsEditable
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The default value for a string combobox.
		/// </summary>
		public string? Default
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The default value for a boolean combobox.
		/// </summary>
		public bool DefaultBoolean
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The title for the <c>true</c> option of a boolean combobox.
		/// </summary>
		public string? TrueTitle
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The title for the <c>false</c> option of a boolean combobox.
		/// </summary>
		public string? FalseTitle
		{
			get;
			set => SetProperty(ref field, value);
		}

		public override ParameterSchemaValue CreateOrFixValue(ParameterSchemaValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (ValueType == ParameterSchemaLimitationType.Boolean)
			{
				if (existing is ParameterSchemaBooleanValue booleanValue)
					return booleanValue;

				log.Append(new ParameterValidationLogEntry
				{
					Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
					OriginalValue = existing?.TakeValueSnapshot(),
					FinalValue = DefaultBoolean
				});
				return new ParameterSchemaBooleanValue
				{
					Value = DefaultBoolean
				};
			}

			if (existing is ParameterSchemaStringValue stringValue &&
				stringValue.Value is not null &&
				(IsEditable || Choices is null || Choices.Contains(stringValue.Value)))
			{
				return stringValue;
			}

			var final = existing is ParameterSchemaStringValue existingString && existingString.Value is not null
				? existingString.Value
				: Default ?? Choices?.FirstOrDefault() ?? string.Empty;

			log.Append(new ParameterValidationLogEntry
			{
				Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
				OriginalValue = existing?.TakeValueSnapshot(),
				FinalValue = final
			});
			return new ParameterSchemaStringValue
			{
				Value = final
			};
		}

		public override Control CreateControl(ParameterSchemaValue value)
		{
			if (ValueType == ParameterSchemaLimitationType.Boolean)
				return CreateBooleanControl((ParameterSchemaBooleanValue)value);

			return CreateStringControl((ParameterSchemaStringValue)value);
		}

		private Control CreateStringControl(ParameterSchemaStringValue stringValue)
		{
			var comboBox = new ComboBox
			{
				Classes = { ParameterElementsStyles.ComboBox },
				ItemsSource = Choices,
				IsEditable = IsEditable
			};

			if (IsEditable)
				comboBox[!ComboBox.TextProperty] = CreateBinding(stringValue, nameof(stringValue.Value), BindingMode.TwoWay);
			else
				comboBox[!ComboBox.SelectedItemProperty] = CreateBinding(stringValue, nameof(stringValue.Value), BindingMode.TwoWay);

			return WrapControl(comboBox);
		}

		private Control CreateBooleanControl(ParameterSchemaBooleanValue booleanValue)
		{
			var items = new List<ParameterBooleanComboItem>
			{
				new() { Value = true, Title = TrueTitle ?? "True" },
				new() { Value = false, Title = FalseTitle ?? "False" }
			};

			var comboBox = new ComboBox
			{
				Classes = { ParameterElementsStyles.ComboBox },
				ItemsSource = items
			};
			comboBox.SelectionChanged += (_, _) =>
			{
				if (comboBox.SelectedItem is ParameterBooleanComboItem item)
					booleanValue.Value = item.Value;
			};
			comboBox.SelectedItem = items.FirstOrDefault(i => i.Value == booleanValue.Value) ?? items[0];

			return WrapControl(comboBox);
		}

		private sealed class ParameterBooleanComboItem
		{
			public required bool Value { get; init; }

			public required string Title { get; init; }

			public override string ToString() => Title;
		}
	}
}
