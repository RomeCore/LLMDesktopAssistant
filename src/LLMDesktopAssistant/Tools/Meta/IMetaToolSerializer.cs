using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Tools.Meta
{
	public interface IMetaToolSerializer
	{
		/// <summary>
		/// Deserializes a meta tool from file contents.
		/// </summary>
		/// <param name="fileContents">The raw contents of the tool file.</param>
		/// <param name="name">The name of the tool (derived from file name).</param>
		/// <param name="isLocal">Indicates if the tool is local- or app-scoped.</param>
		/// <returns>The deserialized meta tool.</returns>
		MetaTool Deserialize(string fileContents, string name, bool isLocal, IMetaToolEngineDescriptor engineDescriptor);

		/// <summary>
		/// Serializes a meta tool to file contents for storage.
		/// </summary>
		/// <param name="tool">The meta tool to serialize.</param>
		/// <returns>The file content to write.</returns>
		string Serialize(MetaTool tool, IMetaToolEngineDescriptor engineDescriptor);
	}
}
