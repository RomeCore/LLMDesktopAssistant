using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LLMDesktopAssistant.SourceGenerators
{
	/// <summary>
	/// Generates inherited settings members for settings categories marked with
	/// <see cref="SettingsRouteAttribute"/>. For every property marked with
	/// <see cref="InheritedChatAgentSettingAttribute"/> or <see cref="InheritedChatSettingAttribute"/>
	/// the generator emits an inheritance level property, an effective value getter
	/// and a write router.
	/// </summary>
	[Generator(LanguageNames.CSharp)]
	public sealed class InheritedSettingsGenerator : IIncrementalGenerator
	{
		private const string RouteAttributeName = "LLMDesktopAssistant.SourceGenerators.SettingsRouteAttribute";
		private const string AgentSettingAttributeName = "LLMDesktopAssistant.SourceGenerators.InheritedChatAgentSettingAttribute";
		private const string ChatSettingAttributeName = "LLMDesktopAssistant.SourceGenerators.InheritedChatSettingAttribute";

		private const string InheritanceLevelType = "global::LLMDesktopAssistant.LLM.Settings.ChatSettingsInheritanceLevel";
		private const string ApplicationSettingsType = "global::LLMDesktopAssistant.Settings.Application.ApplicationSettings";
		private const string ChatSettingsType = "global::LLMDesktopAssistant.LLM.Settings.ChatSettings";
		private const string SettingsManagerType = "global::LLMDesktopAssistant.Settings.SettingsManager";
		private const string NotifyPropertyChangedType = "LLMDesktopAssistant.NotifyPropertyChanged";

		private static readonly SymbolDisplayFormat _typeFormat = SymbolDisplayFormat.FullyQualifiedFormat
			.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

		private static readonly DiagnosticDescriptor _classNotPartial = new(
			"DASSGEN001",
			"Settings category class must be partial",
			"Class '{0}' must be declared as 'partial' to allow source generation of inherited settings members",
			"LLMDesktopAssistant.SourceGenerators",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor _invalidRoute = new(
			"DASSGEN002",
			"Settings route must be a valid property name",
			"SettingsRoute attribute on class '{0}' must specify a single property name, for example nameof(ChatAgentDescriptor.Prompts)",
			"LLMDesktopAssistant.SourceGenerators",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor _unsupportedProperty = new(
			"DASSGEN003",
			"Inherited settings property is not supported",
			"Property '{0}' must be a readable and writable instance property (collections, indexers and static properties are not supported)",
			"LLMDesktopAssistant.SourceGenerators",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor _memberNameConflict = new(
			"DASSGEN004",
			"Generated member name conflicts with an existing member",
			"Member '{0}' already exists in class '{1}'. Remove the manual definition - it is now generated.",
			"LLMDesktopAssistant.SourceGenerators",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor _missingNotifyBase = new(
			"DASSGEN005",
			"Settings category class must inherit from NotifyPropertyChanged",
			"Class '{0}' must inherit from LLMDesktopAssistant.NotifyPropertyChanged to allow source generation of inherited settings members",
			"LLMDesktopAssistant.SourceGenerators",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor _nestedClass = new(
			"DASSGEN006",
			"Nested settings category classes are not supported",
			"Class '{0}' must be a top-level class to allow source generation of inherited settings members",
			"LLMDesktopAssistant.SourceGenerators",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		/// <inheritdoc/>
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(static ctx =>
				ctx.AddSource("InheritedSettingsAttributes.g.cs", SourceText.From(LoadAttributeSource(), Encoding.UTF8)));

			var models = context.SyntaxProvider.ForAttributeWithMetadataName(
				RouteAttributeName,
				static (node, _) => node is ClassDeclarationSyntax,
				static (ctx, ct) => GetClassModel(ctx, ct));

			context.RegisterSourceOutput(models, static (spc, model) => Execute(model, spc));
		}

		private static string LoadAttributeSource()
		{
			var assembly = typeof(InheritedSettingsGenerator).Assembly;
			using var stream = assembly.GetManifestResourceStream("LLMDesktopAssistant.SourceGenerators.InheritedSettingsAttributes.cs")
				?? throw new InvalidOperationException("Embedded attribute source not found.");
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		private static ClassModel GetClassModel(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();

			var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
			var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

			if (!IsPartial(classSymbol))
				diagnostics.Add(new DiagnosticInfo(_classNotPartial, classSymbol.Locations.FirstOrDefault(), classSymbol.Name));

			if (classSymbol.ContainingType is not null)
				diagnostics.Add(new DiagnosticInfo(_nestedClass, classSymbol.Locations.FirstOrDefault(), classSymbol.Name));

			if (!HasNotifyPropertyChangedBase(classSymbol))
				diagnostics.Add(new DiagnosticInfo(_missingNotifyBase, classSymbol.Locations.FirstOrDefault(), classSymbol.Name));

			var route = ctx.Attributes
				.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == RouteAttributeName)?
				.ConstructorArguments.FirstOrDefault().Value as string;

			if (string.IsNullOrWhiteSpace(route) || !SyntaxFacts.IsValidIdentifier(route!))
				diagnostics.Add(new DiagnosticInfo(_invalidRoute, classSymbol.Locations.FirstOrDefault(), classSymbol.Name));

			var properties = ImmutableArray.CreateBuilder<PropertyModel>();

			foreach (var member in classSymbol.GetMembers())
			{
				if (member is not IPropertySymbol property)
					continue;

				var settingInfo = GetSettingInfo(property, ct);
				if (settingInfo is null)
					continue;

				if (property.IsStatic || property.IsIndexer || property.GetMethod is null || property.SetMethod is null)
				{
					diagnostics.Add(new DiagnosticInfo(_unsupportedProperty, property.Locations.FirstOrDefault(), property.Name));
					continue;
				}

				var type = property.Type.ToDisplayString(_typeFormat);

				var inheritanceName = property.Name + "Inheritance";
				var getterName = "GetEffective" + property.Name;
				var setterName = "SetEffective" + property.Name;

				if (classSymbol.GetMembers(inheritanceName).Length > 0 ||
					classSymbol.GetMembers(getterName).Length > 0 ||
					classSymbol.GetMembers(setterName).Length > 0)
				{
					diagnostics.Add(new DiagnosticInfo(_memberNameConflict, property.Locations.FirstOrDefault(), property.Name, classSymbol.Name));
					continue;
				}

				properties.Add(new PropertyModel(property.Name, type, settingInfo.Value.Kind, settingInfo.Value.DefaultLevel));
			}

			return new ClassModel(
				classSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : classSymbol.ContainingNamespace.ToDisplayString(),
				classSymbol.Name,
				route ?? string.Empty,
				properties.ToImmutable(),
				diagnostics.ToImmutable());
		}

		private static void Execute(ClassModel model, SourceProductionContext spc)
		{
			foreach (var diagnostic in model.Diagnostics)
				spc.ReportDiagnostic(diagnostic.ToDiagnostic());

			if (model.Properties.IsEmpty)
				return;

			spc.AddSource($"{model.ClassName}.InheritedSettings.g.cs", SourceText.From(BuildSource(model), Encoding.UTF8));
		}

		private static string BuildSource(ClassModel model)
		{
			var sb = new StringBuilder();
			sb.AppendLine("// <auto-generated/>");
			sb.AppendLine("#nullable enable");
			sb.AppendLine();

			if (model.Namespace.Length > 0)
			{
				sb.Append("namespace ").Append(model.Namespace).AppendLine();
				sb.AppendLine("{");
			}

			sb.Append("\tpartial class ").Append(model.ClassName).AppendLine();
			sb.AppendLine("\t{");

			foreach (var property in model.Properties)
				AppendProperty(sb, property, model.Route);

			sb.AppendLine("\t}");

			if (model.Namespace.Length > 0)
				sb.AppendLine("}");

			return sb.ToString();
		}

		private static void AppendProperty(StringBuilder sb, PropertyModel property, string route)
		{
			var level = InheritanceLevelType;
			var defaultLevel = property.DefaultLevel;
			var fieldName = "_" + ToCamelCase(property.Name) + "Inheritance";

			// Inheritance level field + property
			sb.AppendLine();
			sb.Append("\t\tprivate ").Append(level).Append(' ').Append(fieldName)
				.Append(" = ").Append(level).Append('.').Append(defaultLevel).Append(';');
			sb.AppendLine();
			sb.Append("\t\tpublic ").Append(level).Append(' ').Append(property.Name).Append("Inheritance").AppendLine();
			sb.AppendLine("\t\t{");
			sb.Append("\t\t\tget => ").Append(fieldName).Append(';').AppendLine();
			sb.AppendLine("\t\t\tset");
			sb.AppendLine("\t\t\t{");
			sb.Append("\t\t\t\tif (SetProperty(ref ").Append(fieldName).Append(", value))").AppendLine();
			sb.Append("\t\t\t\t\tRaisePropertyChanged(nameof(").Append(property.Name).Append("));").AppendLine();
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");

			// GetEffective
			sb.AppendLine();
			sb.Append("\t\tpublic ").Append(property.Type).Append(" GetEffective").Append(property.Name).Append('(');
			if (property.Kind == SettingKind.Agent)
				sb.Append(ChatSettingsType).Append(" chatSettings");
			sb.AppendLine(")");
			sb.AppendLine("\t\t{");
			sb.Append("\t\t\tvar appSettings = ").Append(SettingsManagerType).Append(".Get<").Append(ApplicationSettingsType).Append(">();").AppendLine();
			sb.AppendLine();
			sb.Append("\t\t\tswitch (").Append(property.Name).Append("Inheritance)").AppendLine();
			sb.AppendLine("\t\t\t{");

			if (property.Kind == SettingKind.Agent)
			{
				sb.Append("\t\t\t\tcase ").Append(level).Append(".Profile:").AppendLine();
				sb.Append("\t\t\t\t\treturn chatSettings.InheritedAgentSettings.").Append(route).Append('.').Append(property.Name).Append(';').AppendLine();
			}

			sb.Append("\t\t\t\tcase ").Append(level).Append(".Application:").AppendLine();
			sb.Append("\t\t\t\t\treturn appSettings.InheritedChatSettings");
			if (property.Kind == SettingKind.Agent)
				sb.Append(".InheritedAgentSettings");
			sb.Append('.').Append(route).Append('.').Append(property.Name).Append(';').AppendLine();
			sb.AppendLine("\t\t\t\tdefault:");
			sb.Append("\t\t\t\t\treturn ").Append(property.Name).Append(';').AppendLine();
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");

			// SetEffective
			sb.AppendLine();
			sb.Append("\t\tpublic void SetEffective").Append(property.Name).Append('(');
			if (property.Kind == SettingKind.Agent)
				sb.Append(ChatSettingsType).Append(" chatSettings, ");
			sb.Append(property.Type).Append(" value)").AppendLine();
			sb.AppendLine("\t\t{");
			sb.Append("\t\t\tvar appSettings = ").Append(SettingsManagerType).Append(".Get<").Append(ApplicationSettingsType).Append(">();").AppendLine();
			sb.AppendLine();
			sb.Append("\t\t\tswitch (").Append(property.Name).Append("Inheritance)").AppendLine();
			sb.AppendLine("\t\t\t{");

			if (property.Kind == SettingKind.Agent)
			{
				sb.Append("\t\t\t\tcase ").Append(level).Append(".Profile:").AppendLine();
				sb.Append("\t\t\t\t\tchatSettings.InheritedAgentSettings.").Append(route).Append('.').Append(property.Name).Append(" = value;").AppendLine();
				sb.AppendLine("\t\t\t\t\tbreak;");
			}

			sb.Append("\t\t\t\tcase ").Append(level).Append(".Application:").AppendLine();
			sb.Append("\t\t\t\t\tappSettings.InheritedChatSettings");
			if (property.Kind == SettingKind.Agent)
				sb.Append(".InheritedAgentSettings");
			sb.Append('.').Append(route).Append('.').Append(property.Name).Append(" = value;").AppendLine();
			sb.AppendLine("\t\t\t\t\tbreak;");
			sb.AppendLine("\t\t\t\tdefault:");
			sb.Append("\t\t\t\t\t").Append(property.Name).Append(" = value;").AppendLine();
			sb.AppendLine("\t\t\t\t\tbreak;");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
		}

		private static (SettingKind Kind, string DefaultLevel)? GetSettingInfo(IPropertySymbol property, CancellationToken ct)
		{
			foreach (var attribute in property.GetAttributes())
			{
				var name = attribute.AttributeClass?.ToDisplayString();
				if (name == AgentSettingAttributeName)
					return (SettingKind.Agent, GetDefaultLevel(attribute, ct, "Agent"));
				if (name == ChatSettingAttributeName)
					return (SettingKind.Chat, GetDefaultLevel(attribute, ct, "Profile"));
			}

			return null;
		}

		private static string GetDefaultLevel(AttributeData attribute, CancellationToken ct, string fallback)
		{
			var reference = attribute.ApplicationSyntaxReference;
			if (reference is null)
				return fallback;

			if (reference.GetSyntax(ct) is not AttributeSyntax syntax || syntax.ArgumentList is null)
				return fallback;

			foreach (var argument in syntax.ArgumentList.Arguments)
			{
				// The named argument "DefaultLevel" or the single positional argument.
				if (argument.NameEquals is not null &&
					argument.NameEquals.Name.Identifier.ValueText != "DefaultLevel")
				{
					continue;
				}

				return argument.Expression switch
				{
					MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
					IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
					_ => fallback,
				};
			}

			return fallback;
		}

		private static bool IsPartial(INamedTypeSymbol symbol)
		{
			foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
			{
				if (syntaxReference.GetSyntax() is ClassDeclarationSyntax declaration &&
					declaration.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)))
				{
					return true;
				}
			}

			return false;
		}

		private static bool HasNotifyPropertyChangedBase(INamedTypeSymbol symbol)
		{
			for (var type = symbol.BaseType; type is not null; type = type.BaseType)
			{
				if (type.ToDisplayString() == NotifyPropertyChangedType)
					return true;
			}

			return false;
		}

		private static string ToCamelCase(string name)
		{
			return char.ToLowerInvariant(name[0]) + name.Substring(1);
		}

		private enum SettingKind
		{
			Agent,
			Chat
		}

		private sealed record PropertyModel(string Name, string Type, SettingKind Kind, string DefaultLevel);

		private sealed record ClassModel(
			string Namespace,
			string ClassName,
			string Route,
			ImmutableArray<PropertyModel> Properties,
			ImmutableArray<DiagnosticInfo> Diagnostics);

		private sealed class DiagnosticInfo
		{
			private readonly DiagnosticDescriptor _descriptor;
			private readonly Location? _location;
			private readonly object?[] _args;

			public DiagnosticInfo(DiagnosticDescriptor descriptor, Location? location, params object?[] args)
			{
				_descriptor = descriptor;
				_location = location;
				_args = args;
			}

			public Diagnostic ToDiagnostic()
			{
				return Diagnostic.Create(_descriptor, _location, _args);
			}
		}
	}
}
