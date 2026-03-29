using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.LLM.Services
{
	public class DefaultLLModelList : ILLModelList
	{
		static readonly DeepSeekClient deepseek = new(new EnvironmentTokenAccessor("DEEPSEEK_API_KEY"));
		static readonly OpenAICompatibleClient openrouter = new("https://openrouter.ai/api/v1", new EnvironmentTokenAccessor("OPENROUTER_API_KEY"));

		static readonly LLModelDescriptor deepseek_chat = new(deepseek, "deepseek-chat", "DeepSeek Chat");
		static readonly LLModelDescriptor deepseek_reasoner = new(deepseek, "deepseek-reasoner", "DeepSeek Reasoner");
		static readonly LLModelDescriptor mimo_v2_pro = new(openrouter, "xiaomi/mimo-v2-pro", "Xiaomi: MiMo-V2-Pro");
		static readonly LLModelDescriptor mimo_v2_omni = new(openrouter, "xiaomi/mimo-v2-omni", "Xiaomi: MiMo-V2-Omni");
		static readonly LLModelDescriptor step_3_5_flash = new(openrouter, "stepfun/step-3.5-flash:free", "StepFun: Step 3.5 Flash (free)");

		public event Action? ModelsChanged;

		public async Task<IEnumerable<LLModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default)
		{
			return [deepseek_chat, deepseek_reasoner, mimo_v2_pro, mimo_v2_omni, step_3_5_flash];
		}
	}
}