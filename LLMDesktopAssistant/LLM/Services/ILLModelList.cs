using RCLargeLanguageModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface ILLModelList
	{
		/// <summary>
		/// Event raised when the list of models changes.
		/// </summary>
		event Action ModelsChanged;

		/// <summary>
		/// Gets the list of models asynchronously.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to use for the operation.</param>
		/// <returns>A task that represents the asynchronous operation and returns a list of models.</returns>
		Task<IEnumerable<LLModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default);
	}
}