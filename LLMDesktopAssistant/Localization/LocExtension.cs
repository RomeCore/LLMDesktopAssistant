using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
using LLMDesktopAssistant.Modules;

namespace LLMDesktopAssistant.Localization
{
	/// <summary>
	/// XAML markup extension for localizing strings using the LocalizationManager.
	/// </summary>
	public class LocExtension : MarkupExtension
	{
		private readonly string? _key;
		private readonly Binding? _binding;
		private DependencyObject? _targetObject;
		private DependencyProperty? _targetProperty;

		/// <summary>
		/// Initializes a new instance with a static key.
		/// </summary>
		public LocExtension(string key)
		{
			_key = key;
		}

		/// <summary>
		/// Initializes a new instance with a binding to provide dynamic keys.
		/// </summary>
		public LocExtension(Binding keyBinding)
		{
			_binding = keyBinding;
		}

		/// <inheritdoc/>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			var provideValueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
			_targetObject = provideValueTarget?.TargetObject as DependencyObject;
			_targetProperty = provideValueTarget?.TargetProperty as DependencyProperty;

			if (_targetObject == null || _targetProperty == null)
			{
				return this;
			}

			var multiBinding = new MultiBinding();

			if (_binding != null)
			{
				multiBinding.Bindings.Add(_binding);
			}
			else
			{
				multiBinding.Bindings.Add(new Binding
				{
					Source = _key,
					Mode = BindingMode.OneTime
				});
			}

			if (ModuleManager.TryGet<LocalizationManager>() is LocalizationManager localizationManager)
			{
				multiBinding.Bindings.Add(new Binding
				{
					Source = localizationManager,
					Path = new PropertyPath("CurrentLanguage"),
					Mode = BindingMode.OneWay
				});
			}

			multiBinding.Converter = new LocalizationConverter();
			multiBinding.ConverterParameter = StringFormat;

			return multiBinding.ProvideValue(serviceProvider);
		}

		/// <summary>
		/// Optional string format.
		/// </summary>
		public string? StringFormat { get; set; }

		/// <summary>
		/// Optional converter for additional processing.
		/// </summary>
		public IValueConverter? Converter { get; set; }

		/// <summary>
		/// Converter parameter.
		/// </summary>
		public object? ConverterParameter { get; set; }

		private class LocalizationConverter : IMultiValueConverter
		{
			public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
			{
				if (values.Length < 2) return null;

				var key = values[0]?.ToString();
				if (string.IsNullOrEmpty(key)) return null;

				var localizedValue = LocalizationManager.LocalizeStatic(key);

				if (parameter is string format && !string.IsNullOrEmpty(format))
				{
					localizedValue = string.Format(format, localizedValue);
				}

				return localizedValue;
			}

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
				=> throw new NotSupportedException();
		}
	}
}