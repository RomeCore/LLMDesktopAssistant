using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	public partial class AgentPromptSettingsView : UserControl
	{
		public AgentPromptSettingsView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void HintItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var hintItemsControl = sender as ItemsControl;
			var parent = hintItemsControl?.Parent as Panel;
			var behaviorSlider = parent?.Children.FirstOrDefault(c => c.Name == "BehaviorSlider") as Slider;
			if (hintItemsControl == null || behaviorSlider == null)
				return;

			const double columnSpacing = 0;

			if (e.NewSize.Width > 0 &&
				hintItemsControl.Items.Count > 0 &&
				hintItemsControl.DataContext is BehaviorSliderItemViewModel viewModel)
			{
				var columns = viewModel.Range;
				var totalSpacing = (columns - 1) * columnSpacing;
				var columnWidth = (e.NewSize.Width - totalSpacing) / columns;

				behaviorSlider.Margin = new Thickness(columnWidth / 2, 0, columnWidth / 2, 0);
			}
		}
	}
}