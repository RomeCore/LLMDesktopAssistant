namespace LLMDesktopAssistant.Agents.Tasks.MVVM
{
	[Flags]
	public enum AgentTaskListFiltering
	{
		None = 0,

		/// <summary>
		/// Display all tasks, including those bound to chat or message.
		/// </summary>
		All = Parented | App | Chat | Message,

		/// <summary>
		/// Display all tasks, excluding those bound to chat or message but excluding those with a parent task.
		/// </summary>
		AllNotParented = App | Chat | Message,

		/// <summary>
		/// Display tasks that has a parent task.
		/// </summary>
		Parented = 1 << 0,

		/// <summary>
		/// Display tasks that are not bound to either chat or message.
		/// </summary>
		App = 1 << 1,

		/// <summary>
		/// Display tasks that are bound to chat but not bound to message.
		/// </summary>
		Chat = 1 << 2,

		/// <summary>
		/// Display tasks that are bound to message but not bound to chat.
		/// </summary>
		Message = 1 << 3
	}
}
