using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using LLMDesktopAssistant.Prompting.Parameterization;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.Controls
{
	/// <summary>
	/// Reusable control that renders a parameter editor for a <see cref="ParameterSchema"/> and
	/// the corresponding <see cref="ReactiveNodeValue"/>. The editor is rebuilt whenever the
	/// schema or the value changes, so it can be safely reused for different prompt parts.
	/// </summary>
	public partial class PromptPartParameterEditorControl : UserControl
	{
		/// <summary>
		/// Defines the <see cref="Schema"/> property.
		/// </summary>
		public static readonly StyledProperty<ParameterSchema?> SchemaProperty =
			AvaloniaProperty.Register<PromptPartParameterEditorControl, ParameterSchema?>(nameof(Schema));

		/// <summary>
		/// Defines the <see cref="Value"/> property. Bound two-way by default.
		/// </summary>
		public static readonly StyledProperty<ReactiveNodeValue?> ValueProperty =
			AvaloniaProperty.Register<PromptPartParameterEditorControl, ReactiveNodeValue?>(
				nameof(Value), defaultBindingMode: BindingMode.TwoWay);

		/// <summary>
		/// Gets or sets the parameter schema to render.
		/// </summary>
		public ParameterSchema? Schema
		{
			get => GetValue(SchemaProperty);
			set => SetValue(SchemaProperty, value);
		}

		/// <summary>
		/// Gets or sets the parameter value to edit.
		/// </summary>
		public ReactiveNodeValue? Value
		{
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		static PromptPartParameterEditorControl()
		{
			SchemaProperty.Changed.AddClassHandler<PromptPartParameterEditorControl>((o, _) => o.Rebuild());
			ValueProperty.Changed.AddClassHandler<PromptPartParameterEditorControl>((o, _) => o.Rebuild());
		}

		public PromptPartParameterEditorControl()
		{
			InitializeComponent();
			Rebuild();
		}

		private void Rebuild()
		{
			if (ParameterHost is null)
				return;

			if (Schema is null)
			{
				ParameterHost.Content = null;
				return;
			}

			Value = Schema.Root.CreateOrFixValue(Value, []);
			ParameterHost.Content = Schema.Root.CreateControl(Value);
		}
	}
}
