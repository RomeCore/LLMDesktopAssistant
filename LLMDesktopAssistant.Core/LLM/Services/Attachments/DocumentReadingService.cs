using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace LLMDesktopAssistant.Core.LLM.Services.Attachments
{
	public class DocumentReadingService : IDocumentReadingService
	{
		public string ExtractText(string filePath, int startPage, int pageCount)
		{
			var extension = Path.GetExtension(filePath).ToLower();

			return extension switch
			{
				".pdf" => ExtractTextFromPdf(filePath, startPage, pageCount),
				".docx" or ".doc" => ExtractTextFromDocx(filePath, startPage, pageCount),
				".pptx" or ".ppt" => ExtractTextFromPptx(filePath, startPage, pageCount),
				_ => throw new NotSupportedException($"Unsupported file type: {extension}"),
			};
		}

		private static string ExtractTextFromPdf(string filePath, int startPage, int pageCount)
		{
			using (var document = PdfDocument.Open(filePath))
			{
				int numberOfPages = document.NumberOfPages;
				int currentPage = 1;
				List<string> pagesText = [ $"Number of pages: {numberOfPages}" ];
				
				foreach (Page page in document.GetPages())
				{
					if (currentPage >= startPage)
					{
						if (pagesText.Count > pageCount)
							break;

						string text = ContentOrderTextExtractor.GetText(page);
						pagesText.Add($"""
							[PAGE {currentPage}]
							{text.Trim()}
							""");
					}

					currentPage++;
				}

				return string.Join(Environment.NewLine, pagesText);
			}
		}

		private static string ExtractTextFromDocx(string filePath, int startPage, int pageCount)
		{
			using (var document = WordprocessingDocument.Open(filePath, false))
			{
				var elements = document.MainDocumentPart?.Document?.Descendants<Paragraph>().ToArray() ?? [];

				// Assume a page is about 1024 characters long, since we cannot determine the exact number of characters per page.
				const int charsInPage = 1024;

				int currentPage = 1;
				int currentCharCount = 0;
				var currentPageText = new StringBuilder();
				var resultPages = new List<string>();

				foreach (var element in elements)
				{
					string text = element.InnerText;
					if (string.IsNullOrWhiteSpace(text))
						continue;

					int textLength = text.Length;

					if (currentCharCount + textLength > charsInPage && currentCharCount > 0)
					{
						if (currentPage >= startPage)
						{
							resultPages.Add($"[PAGE {currentPage}]\n{currentPageText.ToString().Trim()}");
							if (resultPages.Count >= pageCount)
								break;
						}

						currentPage++;
						currentCharCount = 0;
						currentPageText.Clear();
					}

					if (currentPage >= startPage)
					{
						currentPageText.AppendLine(text);
					}
					currentCharCount += textLength;
				}

				if (currentPage >= startPage && resultPages.Count < pageCount && currentPageText.Length > 0)
				{
					resultPages.Add($"[PAGE {currentPage}]\n{currentPageText.ToString().Trim()}");
				}

				return string.Join(Environment.NewLine + Environment.NewLine, resultPages);
			}
		}

		private static string ExtractTextFromPptx(string filePath, int startPage, int pageCount)
		{
			using (var presentationDocument = PresentationDocument.Open(filePath, false))
			{
				var presentationPart = presentationDocument.PresentationPart;

				if (presentationPart is not null && presentationPart.Presentation is not null)
				{
					var presentation = presentationPart.Presentation;

					if (presentation.SlideIdList is not null)
					{
						OpenXmlElementList slideIds = presentation.SlideIdList.ChildElements;

						int numberOfSlides = slideIds.Count;
						int currentSlide = 1;
						List<string> slidesText = [$"Number of slides: {currentSlide}"];

						while (currentSlide <= slideIds.Count)
						{
							if (slidesText.Count > pageCount)
								break;

							if (currentSlide >= startPage)
							{
								string? slidePartRelationshipId = ((SlideId)slideIds[currentSlide - 1]).RelationshipId;
								if (slidePartRelationshipId is null)
								{
									slidesText.Add($"Invalid relationship ID for slide {currentSlide}.");
									currentSlide++;
									continue;
								}

								var slidePart = (SlidePart)presentationPart.GetPartById(slidePartRelationshipId);
								if (slidePart.Slide is null)
								{
									slidesText.Add($"No slide data found for slide {currentSlide}.");
									currentSlide++;
									continue;
								}

								var text = string.Join(Environment.NewLine, slidePart.Slide.Descendants<Paragraph>()
									.Select(e => e.InnerText.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)));
								slidesText.Add($"""
									[SLIDE {currentSlide}]
									{text}
									""");
								currentSlide++;
							}

						}
					}
				}

				throw new InvalidDataException("No slides found in presentation.");
			}
		}
	}
}