using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tools.Consents
{
	/// <summary>
	/// A UI-friendly memorization option for a tool consent decision.
	/// </summary>
	/// <param name="Memorization">The memorization scope.</param>
	/// <param name="DisplayName">The localized display name of the option.</param>
	public record ToolMemorizationOption(ToolApprovalMemorization Memorization, string DisplayName);

	/// <summary>
	/// Provides the standard set of <see cref="ToolMemorizationOption"/> instances with localized display names.
	/// </summary>
	public static class ToolMemorizationOptions
	{
		/// <summary>
		/// Creates the standard memorization options in display order.
		/// </summary>
		/// <returns>The list of memorization options.</returns>
		public static ImmutableList<ToolMemorizationOption> Create() =>
		[
			new(ToolApprovalMemorization.Once, LocalizationManager.LocalizeStatic("tool.call.remember.option.once")),
			new(ToolApprovalMemorization.Session, LocalizationManager.LocalizeStatic("tool.call.remember.option.session")),
			new(ToolApprovalMemorization.Task, LocalizationManager.LocalizeStatic("tool.call.remember.option.task")),
			new(ToolApprovalMemorization.Always, LocalizationManager.LocalizeStatic("tool.call.remember.option.always"))
		];
	}
}
