using LLTSharp;
using RCParsing;
using RCParsing.Building.ParserRules;

namespace LLMDesktopAssistant.Prompting.LLT;

/// <summary>
/// An <see cref="LLTParser"/> variant that enables error recovery on template declarations,
/// so that a syntax error inside one template does not prevent the remaining templates
/// from being parsed and reported.
/// </summary>
public class LLTDiagnosticsParser : LLTParser
{
	/// <inheritdoc/>
	protected override void ModifyParser(ParserBuilder builder)
	{
		// When a template fails to parse, skip to the next valid template declaration
		// instead of aborting the whole parse. Errors before the recovery point are
		// still recorded and can be obtained via CreateErrorGroups().
		builder.GetRule("template").Recovery(r => r.FindNext());

		var mainSequence = (BuildableSequenceParserRule)builder.GetMainRule().BuildingRule!.Value.AsT2();
		mainSequence.Elements.RemoveAt(mainSequence.Elements.Count - 1); // Remove EOF rule
	}
}
