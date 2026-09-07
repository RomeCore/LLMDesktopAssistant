using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Parsers;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.StructuredValues.Parameterization;
using LLTSharp;
using LLTSharp.Metadata;
using LLTSharp.Metadata.Factories;

namespace LLMDesktopAssistant.Prompting.Management
{
	[Service(typeof(ITemplateParser))]
	[Service(typeof(IAddonFileParser<ITemplate>))]
	public class TemplateAddonParser : ITemplateParser, IAddonFileParser<ITemplate>
	{
		private readonly MetadataFactory[] _metadataFactories;
		private readonly LLTParser _lltParser;

		public string[] SupportedExtensions => [ "llt" ];

		public TemplateAddonParser()
		{
			_metadataFactories = [
				new LanguageMetadataFactory(),
				new VersionMetadataFactory(),
				new TargetModelMetadataFactory(),
				new TargetModelFamilyMetadataFactory(),
				new ParameterSchemaTemplateMetadataFactory()
			];

			_lltParser = new();
		}

		public IEnumerable<ITemplate> Parse(string content, AddonPathInfo fileInfo)
		{
			var extension = Path.GetExtension(fileInfo.Path)?.TrimStart('.') ?? string.Empty;
			return Parse(content, extension);
		}

		public IEnumerable<ITemplate> Parse(string content, string extension)
		{
			switch (extension)
			{
				case "llt":
					return _lltParser.Parse(content, _metadataFactories);

				default:
					throw new NotSupportedException($"Extension '{extension}' is not supported.");
			}
		}
	}
}
