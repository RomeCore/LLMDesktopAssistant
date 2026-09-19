namespace LLMDesktopAssistant.Tools.Specifiers;

/// <summary>
/// A single specifier rule: a pattern and the decision applied when the pattern matches the tool arguments.
/// </summary>
public class ToolSpecifierRule : NotifyPropertyChanged
{
	public bool Enabled
	{
		get;
		set => SetProperty(ref field, value);
	} = true;

	/// <summary>
	/// The specifier pattern (for example, <c>git status:*</c> or <c>fs-edit *</c>).
	/// </summary>
	public string Pattern
	{
		get;
		set => SetProperty(ref field, value);
	} = string.Empty;

	/// <summary>
	/// The decision applied when <see cref="Pattern"/> matches the tool arguments.
	/// </summary>
	public SpecifierDecision Decision
	{
		get;
		set => SetProperty(ref field, value);
	} = SpecifierDecision.Allow;
}
