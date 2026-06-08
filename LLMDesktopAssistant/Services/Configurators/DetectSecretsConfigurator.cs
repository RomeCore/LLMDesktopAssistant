using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator]
	public class DetectSecretsConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			services.AddSingleton<DetectSecretsSharp.Core.Scanner>(sp =>
			{
				var scanner = DetectSecretsSharp.Core.Scanner.CreateDefault();
				return scanner;
			});
		}
	}
}