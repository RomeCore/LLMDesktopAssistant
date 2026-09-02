using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization;
using LLMDesktopAssistant.StructuredValues.Parameterization.Controls;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.Controls
{
	/// <summary>
	/// Reusable control that renders a parameter editor for a <see cref="ParameterSchema"/> and
	/// the corresponding <see cref="ReactiveNodeValue"/>. The editor is rebuilt whenever the
	/// schema or the value changes, so it can be safely reused for different schemas and values.
	/// </summary>
	public partial class ParameterEditorControl : UserControl
	{
		/// <summary>
		/// Defines the <see cref="Schema"/> property.
		/// </summary>
		public static readonly StyledProperty<ParameterSchema?> SchemaProperty =
			AvaloniaProperty.Register<ParameterEditorControl, ParameterSchema?>(nameof(Schema));

		/// <summary>
		/// Defines the <see cref="Value"/> property. Bound two-way by default.
		/// </summary>
		public static readonly StyledProperty<ReactiveNodeValue?> ValueProperty =
			AvaloniaProperty.Register<ParameterEditorControl, ReactiveNodeValue?>(
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

		static ParameterEditorControl()
		{
			SchemaProperty.Changed.AddClassHandler<ParameterEditorControl>((o, _) => o.Rebuild());
			ValueProperty.Changed.AddClassHandler<ParameterEditorControl>((o, _) => o.Rebuild());
		}

		public ParameterEditorControl()
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

			var controlFactory = ServiceRegistry.Provider.GetService<IParameterSchemaControlFactoryManager>();
			ParameterHost.Content = controlFactory?.CreateControl(Schema.Root, Value);
		}
	}
}
