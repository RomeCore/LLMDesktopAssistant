using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Parameterization.Values
{
	public abstract class ParameterSchemaValue : NotifyPropertyChanged
	{
		/// <summary>
		/// Get a snapshot of the current value of the parameter.
		/// This is used mostly for logging validation errors.
		/// </summary>
		/// <returns>A snapshot of the current value of the parameter.</returns>
		public abstract object? TakeValueSnapshot();

		/// <summary>
		/// Get a template data accessor for the parameter.
		/// Used for template rendering.
		/// </summary>
		/// <returns>A template data accessor for the parameter.</returns>
		public abstract TemplateDataAccessor GetTemplateDataAccessor();
	}
}
