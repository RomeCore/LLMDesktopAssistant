namespace LLMDesktopAssistant.Tools.Specifiers;

/// <summary>
/// A single specifier rule: a pattern and the decision applied when the pattern matches the tool arguments.
/// </summary>
public class ToolSpecifierRule : NotifyPropertyChanged
{
	private string _pattern = string.Empty;
	/// <summary>
	/// The specifier pattern (for example, <c>git status:*</c> or <c>fs-edit *</c>).
	/// </summary>
	public string Pattern
	{
		get => _pattern;
		set => SetProperty(ref _pattern, value);
	}

	private SpecifierDecision _decision = SpecifierDecision.Allow;
	/// <summary>
	/// The decision applied when <see cref="Pattern"/> matches the tool arguments.
	/// </summary>
	public SpecifierDecision Decision
	{
		get => _decision;
		set => SetProperty(ref _decision, value);
	}
}
