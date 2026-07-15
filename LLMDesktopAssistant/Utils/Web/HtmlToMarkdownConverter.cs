using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Utils.Web
{
	public static class HtmlToMarkdownConverter
	{
		private static readonly ReverseMarkdown.Converter _mdConverter;

		private static ReverseMarkdown.Config CreateMdConfig()
		{
			var result = new ReverseMarkdown.Config
			{
				GithubFlavored = true,
			};

			result.Tags.Unknown = ReverseMarkdown.Config.UnknownTagsOption.Drop;
			result.Formatting.RemoveComments = true;
			result.Images.Base64Handling = ReverseMarkdown.Config.Base64ImageHandling.Skip;

			return result;
		}

		static HtmlToMarkdownConverter()
		{
			_mdConverter = new(CreateMdConfig());
		}

		public static string Convert(string html)
		{
			return _mdConverter.Convert(html);
		}
	}
}
