using System.Text.Json.Serialization;
using LLMDesktopAssistant.StructuredValues.Parameterization;

namespace LLMDesktopAssistant.Addons
{
	public abstract class AddonChangedBase<Self, TChange> : AddonMetadata
		where Self : AddonBase<Self>
		where TChange : AddonChangeBase
	{
		// ===================================
		// === Variable state              ===
		// ===================================

		/// <summary>
		/// Gets a value indicating whether the addon is enabled. Defaults to <see langword="null"/>.
		/// </summary>
		public bool? Enabled
		{
			get;
			set => SetProperty(ref field, value);
		} = null;

		/// <summary>
		/// The change confiuration object that been used to make some changes to this addon instance.
		/// </summary>
		[JsonIgnore]
		public TChange? Change
		{
			get;
			set => SetProperty(ref field, value);
		} = null;

		/// <summary>
		/// The parameter schema of the addon template, if the addon body is a template with @params metadata.
		/// Null for plain-text addons without parameters.
		/// </summary>
		public ParameterSchema? ParameterSchema
		{
			get;
			set => SetProperty(ref field, value);
		} = null;
	}
}
