using System;
using System.Collections.Generic;
using System.Text;
using DetectSecretsSharp.Plugins;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator]
	public class DetectSecretsConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			services.AddSingleton(sp =>
			{
				// ОНО ПИЗДЕЦ МЕДЛЕННОЕ
				// Пришлось оставить всего два детектора ключей
				// Со всеми детекторами ~37 мс на строку, а это пиздец медленно (несколько секунд на файлы)
				var scanner = new DetectSecretsSharp.Core.Scanner([
					//new AwsKeyDetector(),
					//new AzureStorageKeyDetector(),
					//new ArtifactoryDetector(),
					//new BasicAuthDetector(),
					//new CloudantDetector(),
					//new DiscordBotTokenDetector(),
					//new GitHubTokenDetector(),
					//new GitLabTokenDetector(),
					//new IbmCloudIamDetector(),
					//new IbmCosHmacDetector(),
					//new IpPublicDetector(),
					//new JwtTokenDetector(),
					//new KeywordDetector(),
					//new MailchimpDetector(),
					//new NpmDetector(),
					//new OpenAiDetector(),
					//new PrivateKeyDetector(),
					//new PypiTokenDetector(),
					//new SendGridDetector(),
					//new SlackDetector(),
					//new SoftlayerDetector(),
					//new SquareOAuthDetector(),
					//new StripeDetector(),
					//new TelegramBotTokenDetector(),
					//new TwilioKeyDetector(),
					new Base64HighEntropyStringDetector(),
					new HexHighEntropyStringDetector(),
				]);
				return scanner;
			});
		}
	}
}