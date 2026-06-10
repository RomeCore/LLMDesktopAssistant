using System;
using System.Collections.Generic;
using System.Text;
using DocumentFormat.OpenXml.Spreadsheet;
using LLMDesktopAssistant.Calculation.Ast;
using RCParsing;
using RCParsing.TokenPatterns;

namespace LLMDesktopAssistant.Calculation
{
	public static class MathExpressionParser
	{
		private static readonly Parser _parser;

		/// <summary>
		/// Gets the parser for parsing mathematical expressions.
		/// </summary>
		public static Parser Parser => _parser;

		static MathExpressionParser()
		{
			var builder = new ParserBuilder();

			builder.Settings
				.SkipWhitespaces();

			// Basic terms

			builder.CreateRule("number")
				.Number<double>(NumberFlags.UnsignedScientific)

				.Transform(v =>
				{
					var value = v.GetIntermediateValue<double>();
					return new ConstantEntity(value);
				});

			builder.CreateRule("imaginary")
				.KeywordIgnoreCase("i")

				.Transform(v =>
				{
					return new ImaginaryEntity();
				});

			builder.CreateRule("function")
				.UnicodeIdentifier()
				.Literal("(")
				.ZeroOrMoreSeparated(r => r.Rule("expr"), r => r.Literal(","), includeSeparatorsInResult: false)
				.Literal(")")

				.Transform(v =>
				{
					var name = v[0].Text;
					var args = v[2].SelectValues<MathEntity>();
					return new FunctionCallEntity(name, args);
				});

			builder.CreateRule("const_or_variable")
				.Choice(
					b => b.KeywordChoiceIgnoreCase("nan", "pi", "π", "inf", "infinity", "∞", "eps", "epsilon", "ε", "phi", "φ", "tau", "τ", "g", "e", "c", "gamma"),
					b => b.UnicodeIdentifier()
				)
				
				.Transform(v =>
				{
					var str = v.TryGetIntermediateValue<string>() ?? v.Text;
					return str switch
					{
						"nan" => new NamedConstantEntity(NamedConstantKind.NaN),
						"pi" or "π" => new NamedConstantEntity(NamedConstantKind.Pi),
						"inf" or "infinity" or "∞" => new NamedConstantEntity(NamedConstantKind.Infinity),
						"eps" or "epsilon" or "ε" => new NamedConstantEntity(NamedConstantKind.Epsilon),
						"phi" or "φ" => new NamedConstantEntity(NamedConstantKind.Phi),
						"tau" or "τ" => new NamedConstantEntity(NamedConstantKind.Tau),
						"g" => new NamedConstantEntity(NamedConstantKind.G),
						"e" => new NamedConstantEntity(NamedConstantKind.E),
						"c" => new NamedConstantEntity(NamedConstantKind.C),
						"gamma" => new NamedConstantEntity(NamedConstantKind.Gamma),
						_ => new VariableEntity(str)
					};
				});

			// The term

			builder.CreateRule("term")
				.Choice(
					b => b.Rule("number"),
					b => b.Rule("imaginary"),
					b => b.Rule("function"),
					b => b.Rule("const_or_variable"),

					b => b.Literal("(").Rule("expr").Literal(")")
						.Transform(v => v[1].GetValue()),

					b => b.Literal("|").Rule("expr").Literal("|")
						.Transform(v => new UnaryOpEntity(UnaryOpKind.Abs, v[1].GetValue<MathEntity>()))
				)
				.Optional(b => b.Literal("!")) // Factorial
				.Optional(b => b.Rule("term")) // For support 2(2 + 3) or 2i

				.Transform(v =>
				{
					var firstTerm = v.GetValue<MathEntity>(index: 0);
					var secondTerm = v.TryGetValue<MathEntity>(index: 2);
					var hasFactorial = v[1].Length > 0;

					var result = firstTerm;
					if (secondTerm != null)
					{
						result = new BinaryOpEntity(BinaryOpKind.Multiply, firstTerm, secondTerm);
					}
					if (hasFactorial)
					{
						result = new UnaryOpEntity(UnaryOpKind.Factorial, result);
					}
					return result;
				});

			// The expressions

			builder.CreateRule("op_pow")
				.OneOrMoreSeparated(b => b.Rule("term"), b => b.Literal("^"))

				.TransformFoldRight<MathEntity, MathEntity>((l, r) =>
				{
					return new BinaryOpEntity(BinaryOpKind.Power, l, r);
				});

			builder.CreateRule("op_pre")
				.ZeroOrMore(b => b.LiteralChoice("+", "-"))
				.Rule("op_pow")

				.Transform(v =>
				{
					var operators = v.SelectArray<string>(index: 0);
					var value = v.GetValue<MathEntity>(index: 1);

					for (int i = operators.Length - 1; i >= 0; i--)
					{
						var op = operators[i];
						value = op switch
						{
							"-" => new UnaryOpEntity(UnaryOpKind.Minus, value),
							"+" => new UnaryOpEntity(UnaryOpKind.Plus, value),
							_ => value
						};
					}

					return value;
				});

			builder.CreateRule("op_mul")
				.OneOrMoreSeparated(b => b.Rule("op_pre"), b => b.LiteralChoice("*", "/", "%"), includeSeparatorsInResult: true)

				.TransformFoldLeft<MathEntity, string, MathEntity>((l, op, r) =>
				{
					return op switch
					{
						"*" => new BinaryOpEntity(BinaryOpKind.Multiply, l, r),
						"/" => new BinaryOpEntity(BinaryOpKind.Divide, l, r),
						"%" => new BinaryOpEntity(BinaryOpKind.Mod, l, r),
						_ => throw new InvalidOperationException($"Unknown operator: {op}")
					};
				});

			builder.CreateRule("op_add")
				.OneOrMoreSeparated(b => b.Rule("op_mul"), b => b.LiteralChoice("+", "-"), includeSeparatorsInResult: true)

				.TransformFoldLeft<MathEntity, string, MathEntity>((l, op, r) =>
				{
					return op == "+" ? new BinaryOpEntity(BinaryOpKind.Add, l, r) : new BinaryOpEntity(BinaryOpKind.Subtract, l, r);
				});

			builder.CreateRule("expr")
				.Rule("op_add");

			builder.CreateMainRule()
				.Rule("expr").EOF().TransformSelect(0);

			_parser = builder.Build();
		}

		public static MathEntity Parse(string expression)
		{
			return Parser.Parse<MathEntity>(expression);
		}

		public static bool TryParse(string expression, out MathEntity result)
		{
			return _parser.TryParse(expression, out result);
		}
	}
}
