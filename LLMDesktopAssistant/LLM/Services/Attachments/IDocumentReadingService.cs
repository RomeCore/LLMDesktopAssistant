using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace LLMDesktopAssistant.LLM.Services.Attachments
{
	public interface IDocumentReadingService
	{
		string ExtractText(string filePath, int startPage, int pageCount);
	}
}