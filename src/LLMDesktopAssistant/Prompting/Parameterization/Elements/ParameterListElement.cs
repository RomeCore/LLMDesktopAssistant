using Avalonia.Controls;
using LLMDesktopAssistant.Prompting.Parameterization.MVVM;
using LLMDesktopAssistant.Prompting.Parameterization.Values;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.Prompting.Parameterization.Elements
{
	[JsonDerived(typeof(ParameterSchemaElement), "list")]
	public class ParameterListElement : ParameterSchemaElement
	{
		/// <summary>
		/// The schema of the list items.
		/// </summary>
		public required ParameterSchemaElement ItemsSchema
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The minimum number of items in the list.
		/// </summary>
		public int Min
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The maximum number of items in the list.
		/// </summary>
		public int Max
		{
			get;
			set => SetProperty(ref field, value);
		}

		public override ParameterSchemaValue CreateOrFixValue(ParameterSchemaValue? existing, AppendOnlyList<ParameterValidationLogEntry> log)
		{
			if (existing is ParameterSchemaArrayValue arrayValue)
			{
				for (int i = 0; i < arrayValue.Items.Count; i++)
					arrayValue.Items[i] = ItemsSchema.CreateOrFixValue(arrayValue.Items[i], log);

				while (arrayValue.Items.Count < Min)
					arrayValue.Items.Add(ItemsSchema.CreateOrFixValue(null, log));

				while (arrayValue.Items.Count > Max)
				{
					var removed = arrayValue.Items[^1];
					arrayValue.Items.RemoveAt(arrayValue.Items.Count - 1);
					log.Append(new ParameterValidationLogEntry
					{
						Status = ParameterValidationStatus.Fixed,
						OriginalValue = removed.TakeValueSnapshot(),
						FinalValue = null,
						Message = "Removed an excess list item."
					});
				}
				return arrayValue;
			}

			log.Append(new ParameterValidationLogEntry
			{
				Status = existing is null ? ParameterValidationStatus.Created : ParameterValidationStatus.Invalid,
				OriginalValue = existing?.TakeValueSnapshot(),
				FinalValue = null
			});

			var newArray = new ParameterSchemaArrayValue();
			for (int i = 0; i < Min; i++)
				newArray.Items.Add(ItemsSchema.CreateOrFixValue(null, log));
			return newArray;
		}

		public override Control CreateControl(ParameterSchemaValue value)
		{
			var arrayValue = (ParameterSchemaArrayValue)value;
			var viewModel = new ParameterSchemaListViewModel(this, arrayValue);
			return WrapControl(new ContentControl
			{
				Content = viewModel
			});
		}
	}
}
