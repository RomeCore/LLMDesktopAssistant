using LLMDesktopAssistant.LLM.MVVM.Additional;

namespace LLMDesktopAssistant.Prompting.Hooks;

public class AttachedMessageAdditionalViewModel : AdditionalMessageViewModel
{
	private AttachedMessageMode _mode = AttachedMessageMode.Prepend;
	/// <summary>
	/// Gets or sets the mode for attaching additional content to a message.
	/// </summary>
	public AttachedMessageMode Mode
	{
		get => _mode;
		set => SetProperty(ref _mode, value);
	}

	private string? _content;
	/// <summary>
	/// Gets or sets the content to attach to target message.
	/// </summary>
	public string? Content
	{
		get => _content;
		set => SetProperty(ref _content, value);
	}

	public AttachedMessageAdditionalViewModel()
	{
		IsTemporary = false;
		IsVisible = false;
	}
}

