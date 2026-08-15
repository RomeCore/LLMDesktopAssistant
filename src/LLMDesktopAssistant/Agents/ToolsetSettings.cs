using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the toolset configuration for an agent: either a custom toolset stored locally
	/// or a reference to a shared toolset configuration.
	/// </summary>
	public class ToolsetSettings : NotifyPropertyChanged
	{
		private bool _useCustomToolset = true;
		/// <summary>
		/// Gets or sets a value indicating whether the custom <see cref="ToolsetConfiguration"/>
		/// is used instead of the referenced shared configuration.
		/// </summary>
		public bool UseCustomToolset
		{
			get => _useCustomToolset;
			set => SetProperty(ref _useCustomToolset, value);
		}

		private ToolsetConfiguration _toolsetConfiguration = new();
		/// <summary>
		/// Gets or sets the custom toolset configuration local to this agent.
		/// </summary>
		public ToolsetConfiguration ToolsetConfiguration
		{
			get => _toolsetConfiguration;
			set => SetProperty(ref _toolsetConfiguration, value);
		}

		private SettingsReference<ToolsetConfiguration> _reference = new();
		/// <summary>
		/// Gets or sets the reference to a shared toolset configuration resolved through <see cref="SettingsManager"/>.
		/// </summary>
		public SettingsReference<ToolsetConfiguration> Reference
		{
			get => _reference;
			set => SetProperty(ref _reference, value);
		}

		/// <summary>
		/// Returns the toolset configuration that is currently effective: the custom one when
		/// <see cref="UseCustomToolset"/> is <see langword="true"/>, otherwise the referenced
		/// shared configuration, created on demand if it does not exist yet.
		/// </summary>
		public ToolsetConfiguration GetEffectiveConfiguration()
			=> UseCustomToolset ? ToolsetConfiguration : Reference.EnsureObject();
	}
}
