using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Messages;
using Material.Icons;

namespace LLMDesktopAssistant.UIExtensions.MessageExtensions
{
	[MessageExtension]
	public class SaveMessageExtension : MessageExtension
	{
		public override int Order => 1;

		public SaveMessageExtension(MessageViewModelBase viewModel)
		{
			Icon = MaterialIconKind.ContentSave;

			Command = new AsyncRelayCommand(async () =>
			{
				var result = await App.MainTopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
				{
					FileTypeChoices = [
						new FilePickerFileType("Markdown files") { Patterns = ["*.md"] },
						new FilePickerFileType("Any files") { Patterns = ["*.*"] }
					],
					ShowOverwritePrompt = true
				});

				if (result != null)
				{
					var path = result.Path.LocalPath;
					File.WriteAllText(path, viewModel.Message.Content);
				}
			});
		}
	}
}