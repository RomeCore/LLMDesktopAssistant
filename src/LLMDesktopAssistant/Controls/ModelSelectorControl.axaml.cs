using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Services;
using Serilog;

namespace LLMDesktopAssistant.Controls
{
	/// <summary>
	/// Wrapper for a model item in the dropdown list.
	/// </summary>
	public class ModelItemWrapper
	{
		/// <summary>
		/// Gets or sets the full name of the model (e.g. "OpenAI$gpt-4o").
		/// </summary>
		public required string FullName { get; set; }

		/// <summary>
		/// Gets or sets the display text for the model.
		/// </summary>
		public required string DisplayText { get; set; }
	}

	/// <summary>
	/// Wrapper for a header item in the dropdown list (provider group header).
	/// </summary>
	public class ComboBoxHeaderItem
	{
		public required string Title { get; init; }
	}

	/// <summary>
	/// Wrapper for an empty item in the dropdown list.
	/// </summary>
	public class ComboBoxEmptyItem
	{
		public string Title { get; init; } = LocalizationManager.LocalizeStatic("model_selector_no_models");
	}

	/// <summary>
	/// A control for selecting a model from available providers.
	/// Works with model full names in format "ProviderName$ModelName".
	/// </summary>
	public partial class ModelSelectorControl : UserControl
	{
		/// <summary>
		/// Defines the <see cref="SelectedModel"/> property.
		/// </summary>
		public static readonly StyledProperty<string> SelectedModelProperty =
			AvaloniaProperty.Register<ModelSelectorControl, string>(
				nameof(SelectedModel));

		/// <summary>
		/// Gets or sets the selected model full name.
		/// </summary>
		public string SelectedModel
		{
			get => GetValue(SelectedModelProperty);
			set => SetValue(SelectedModelProperty, value);
		}

		/// <summary>
		/// Occurs when the selected model changes.
		/// </summary>
		public event Action<string>? SelectedModelChanged;

		static ModelSelectorControl()
		{
			SelectedModelProperty.Changed.AddClassHandler<ModelSelectorControl>(
				(o, e) => o.OnSelectedModelChanged((string)e.NewValue!));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ModelSelectorControl"/> class.
		/// </summary>
		public ModelSelectorControl()
		{
			InitializeComponent();

			Selector.SelectionChanged += Selector_SelectionChanged;
			Selector.DropDownOpened += (_, _) => Rebuild();

			Rebuild();
		}

		private void OnSelectedModelChanged(string newValue)
		{
			SelectedModelChanged?.Invoke(newValue);

			if (string.IsNullOrEmpty(newValue))
			{
				Selector.SelectedIndex = 0;
				return;
			}

			foreach (var item in Selector.Items)
			{
				if (item is ModelItemWrapper wrapper && wrapper.FullName == newValue)
				{
					Selector.SelectedItem = item;
					return;
				}
			}

			// If not found, add it temporarily
			Selector.Items.Insert(1, new ModelItemWrapper
			{
				FullName = newValue,
				DisplayText = newValue
			});
			Selector.SelectedIndex = 1;
		}

		private void Selector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			if (Selector.SelectedItem is ModelItemWrapper wrapper)
				SelectedModel = wrapper.FullName;
			else
				SelectedModel = string.Empty;
		}

		private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
		{
			var vm = new ManageModelProvidersDialogViewModel();
			await DialogManager.ShowDialogAsync(vm);
			Rebuild();
		}

		private void Rebuild()
		{
			var prevSelected = SelectedModel;

			Selector.Items.Clear();
			Selector.Items.Add(new ModelItemWrapper
			{
				FullName = string.Empty,
				DisplayText = LocalizationManager.LocalizeStatic("model_selector_none")
			});

			try
			{
				var modelManager = ServiceRegistry.Provider.GetRequiredService<IModelManager>();
				var models = modelManager.ListSelectedModels()
					.OrderBy(m => m.Provider.Name)
					.ThenBy(m => m.Descriptor.Name)
					.ToList();

				string? lastProviderName = null;
				bool found = false;

				foreach (var model in models)
				{
					if (model.Provider.Name != lastProviderName)
					{
						lastProviderName = model.Provider.Name;
						Selector.Items.Add(new ComboBoxHeaderItem
						{
							Title = model.Provider.Name
						});
					}

					var wrapper = new ModelItemWrapper
					{
						FullName = model.FullName,
						DisplayText = !string.IsNullOrEmpty(model.Descriptor.DisplayName) ? model.Descriptor.DisplayName : model.Descriptor.Name
					};

					Selector.Items.Add(wrapper);

					if (model.FullName == prevSelected)
					{
						Selector.SelectedItem = wrapper;
						found = true;
					}
				}

				if (!found && !string.IsNullOrEmpty(prevSelected))
				{
					Selector.Items.Add(new ModelItemWrapper
					{
						FullName = prevSelected,
						DisplayText = prevSelected
					});
					Selector.SelectedIndex = Selector.Items.Count - 1;
				}
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Failed to load models for ModelSelectorControl");
				Selector.Items.Add(new ComboBoxEmptyItem
				{
					Title = LocalizationManager.LocalizeStatic("model_selector_failed")
				});
			}

			if (Selector.SelectedItem == null)
				Selector.SelectedIndex = 0;
		}
	}
}
