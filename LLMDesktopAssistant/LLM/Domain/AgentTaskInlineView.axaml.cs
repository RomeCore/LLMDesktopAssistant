using Avalonia;
using Avalonia.Controls;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.LLM.Domain;

/// <summary>
/// View for <see cref="AgentTaskInlineViewModel"/> — delegates rendering to the embedded
/// <see cref="AgentTaskViewModel"/> via a <see cref="ContentControl"/>.
/// </summary>
[ViewFor(typeof(AgentTaskInlineViewModel))]
public partial class AgentTaskInlineView : UserControl
{
	public AgentTaskInlineView()
	{
		InitializeComponent();
	}
}
