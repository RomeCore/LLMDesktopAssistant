using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Tools;
using RCParsing;
using RCParsing.Building;
using RCParsing.Building.ParserRules;

namespace LLMDesktopAssistant.ToolModules
{
	[Module]
	public class CalculationModule : ToolModule
	{
		private static readonly Parser _exprParser = CreateMathExprParser();

		private static RuleBuilder CreateMathFunctionRule(Delegate @delegate, string funcName, string expressionName)
		{
			var builder = new RuleBuilder();

			var argCount = @delegate.Method.GetParameters().Length;

			builder
				.Keyword(funcName)
				.Literal("(")
				.RepeatSeparated(r => r.Rule(expressionName), r => r.Literal(","), min: argCount, max: argCount)
				.Literal(")")

				.Transform(v =>
				{
					var values = v.SelectArray<object>(index: 2);
					var value = @delegate.DynamicInvoke(values)!;
					return (double)value;
				});

			return builder;
		}

		private static Parser CreateMathExprParser(Action<ParserBuilder>? builderAction = null)
		{
			var builder = new ParserBuilder();

			builder.Settings.SkipWhitespaces();

			// Basic terms

			builder.CreateRule("number")
				.Number<double>();

			builder.CreateRule("const")
				.KeywordChoice("pi", "inf", "eps", "e", "nan")

				.Transform(v =>
				{
					return v.GetIntermediateValue<string>() switch
					{
						"pi" => Math.PI,
						"inf" => double.PositiveInfinity,
						"eps" => double.Epsilon,
						"e" => double.E,
						"nan" => double.NaN,
						_ => throw new Exception() // Will not be thrown
					};
				});

			// Functions

			var funcChoiceRuleHolder = builder.CreateRule("func");
			var funcChoiceRule = new BuildableChoiceParserRule();
			funcChoiceRule.ParsedValueFactory = v => v.GetValue(0);
			funcChoiceRuleHolder.BuildingRule = funcChoiceRule;

			void AddFunction(Delegate @delegate, string name)
			{
				funcChoiceRule.Choices.Add(CreateMathFunctionRule(@delegate, name, "expr").BuildingRule!.Value);
			}

			AddFunction((double value) => (double)Math.Log(value), "ln");
			AddFunction((double value) => (double)Math.Log10(value), "log");
			AddFunction((double value) => (double)Math.Log2(value), "log2");
			AddFunction(Math.Sin, "sin");
			AddFunction(Math.Cos, "cos");
			AddFunction(Math.Sqrt, "sqrt");
			AddFunction(Math.Tan, "tan");
			AddFunction(Math.Tanh, "tanh");
			AddFunction(Math.Sinh, "sinh");
			AddFunction(Math.Cosh, "cosh");
			AddFunction((double value) => (double)Math.Sign(value), "sign");

			// The expressions

			builder.CreateRule("term")
				.Choice(
					b => b.Rule("number"),
					b => b.Rule("const"),

					b => b.Literal("(").Rule("expr").Literal(")")
						.Transform(v => v.GetValue<double>(index: 1)),

					b => b.Literal("|").Rule("expr").Literal("|")
						.Transform(v => Math.Abs(v.GetValue<double>(index: 1))),

					b => b.Rule("func")
				);

			builder.CreateRule("op_pre")
				.ZeroOrMore(b => b.LiteralChoice("+", "-"))
				.Rule("term")

				.Transform(v =>
				{
					var operators = v.SelectArray<string>(index: 0);
					var value = v.GetValue<double>(index: 1);

					for (int i = operators.Length - 1; i >= 0; i--)
					{
						var op = operators[i];
						value = op switch
						{
							"-" => -value,
							_ => value
						};
					}

					return value;
				});

			builder.CreateRule("op_pow")
				.OneOrMoreSeparated(b => b.Rule("op_pre"), b => b.Literal("^"))

				.TransformFoldLeft<double, double>((l, r) =>
				{
					return Math.Pow(l, r);
				});

			builder.CreateRule("op_mul")
				.OneOrMoreSeparated(b => b.Rule("op_pow"), b => b.LiteralChoice("*", "/"), includeSeparatorsInResult: true)

				.TransformFoldLeft<double, string, double>((l, op, r) =>
				{
					return op == "*" ? l * r : l / r;
				});

			builder.CreateRule("op_add")
				.OneOrMoreSeparated(b => b.Rule("op_mul"), b => b.LiteralChoice("+", "-"), includeSeparatorsInResult: true)

				.TransformFoldLeft<double, string, double>((l, op, r) =>
				{
					return op == "+" ? l + r : l - r;
				});

			builder.CreateRule("expr")
				.Rule("op_add");

			builder.CreateMainRule()
				.Rule("expr").EOF().TransformSelect(0);

			builderAction?.Invoke(builder);
			return builder.Build();
		}

		private readonly FunctionTool _calculateTool;

		public CalculationModule()
		{
			_calculateTool = FunctionTool.From(Calculate, "calculate", "Evaluate a mathematical expression. Example: '2 + 3 * 4' returns 14.");
		}

		private ToolResult Calculate([Description("Expression to evaluate")] string expression)
		{
			try
			{
				var result = _exprParser.Parse<double>(expression);
				return new ToolResult(result.ToString());
			}
			catch (ParsingException pex)
			{
				return new ToolResult(pex.Message);
			}
		}

		public override IEnumerable<ITool> GetTools()
		{
			return [_calculateTool];
		}
	}
}