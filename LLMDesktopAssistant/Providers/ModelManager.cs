using LLMDesktopAssistant.Services;
using RCLargeLanguageModels;

namespace LLMDesktopAssistant.Providers
{
	[Service(typeof(IModelManager))]
	public class ModelManager : IModelManager
	{
		public bool IsModelAvaliable(string fullName)
		{
			throw new NotImplementedException();
		}

		public LLModel GetModel(string fullName)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<ModelItem> ListModels()
		{
			throw new NotImplementedException();
		}

		public void RefreshModels(ModelProviderConfiguration provider)
		{
			throw new NotImplementedException();
		}

		private static (string ClientName, string ModelName) ParseFullName(string fullName)
		{
			var split = fullName.Split('$', count: 2);
			if (split.Length < 2)
				throw new ArgumentException("Invalid model full name format.", nameof(fullName));
			return (ClientName: split[0], ModelName: split[1]);
		}
	}
}
