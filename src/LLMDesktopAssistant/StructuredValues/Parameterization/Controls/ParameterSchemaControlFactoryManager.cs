using Avalonia.Controls;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization.Elements;
using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.StructuredValues.Parameterization.Controls
{
	[Service(typeof(IParameterSchemaControlFactoryManager))]
	public class ParameterSchemaControlFactoryManager(
		IServiceProvider services
	) : IParameterSchemaControlFactoryManager
	{
		private Dictionary<Type, IParameterSchemaControlFactory> _factories = null!;

		private void EnsureFactoriesInitialized()
		{
			_factories ??= services.GetServices<IParameterSchemaControlFactory>()
				.ToDictionary(f => f.ElementType);
		}

		public Control CreateControl(ParameterSchemaElement element, ReactiveNodeValue value)
		{
			ArgumentNullException.ThrowIfNull(element);
			ArgumentNullException.ThrowIfNull(value);
			EnsureFactoriesInitialized();

			if (!_factories.TryGetValue(element.GetType(), out var factory))
				throw new ArgumentException($"No control factory registered for schema element type '{element.GetType().FullName}'.", nameof(element));

			return factory.CreateControl(element, value);
		}
	}
}
