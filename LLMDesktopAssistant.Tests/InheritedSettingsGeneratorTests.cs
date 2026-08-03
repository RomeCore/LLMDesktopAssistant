using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.Tests;

public class InheritedSettingsGeneratorTests
{
	private static readonly MetadataReference[] _references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
		.Split(Path.PathSeparator)
		.Select(p => MetadataReference.CreateFromFile(p))
		.ToArray();

	private const string SampleSource = """
		namespace LLMDesktopAssistant
		{
			public abstract class NotifyPropertyChanged : System.ComponentModel.INotifyPropertyChanged
			{
				public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged = delegate { };

				protected bool SetProperty<T>(ref T field, T value)
				{
					if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
						return false;
					field = value;
					PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(null));
					return true;
				}

				protected void RaisePropertyChanged(string? propertyName)
					=> PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
			}
		}

		namespace LLMDesktopAssistant.Agents
		{
			public class ChatAgentDescriptor
			{
				public AgentPromptSettings Prompts { get; set; } = new();
			}

			[LLMDesktopAssistant.SourceGenerators.SettingsRoute(nameof(ChatAgentDescriptor.Prompts))]
			public partial class AgentPromptSettings : LLMDesktopAssistant.NotifyPropertyChanged
			{
				private string? _systemPrompt;

				[LLMDesktopAssistant.SourceGenerators.InheritedChatAgentSetting]
				public string? SystemPrompt
				{
					get => _systemPrompt;
					set => SetProperty(ref _systemPrompt, value);
				}
			}
		}

		namespace LLMDesktopAssistant.LLM.Settings
		{
			public enum ChatSettingsInheritanceLevel
			{
				Application,
				Profile,
				Agent
			}

			public class ChatSettings : LLMDesktopAssistant.NotifyPropertyChanged
			{
				public LLMDesktopAssistant.Agents.ChatAgentDescriptor InheritedAgentSettings { get; set; } = new();
				public ChatModelSettings Models { get; set; } = new();
			}

			[LLMDesktopAssistant.SourceGenerators.SettingsRoute(nameof(ChatSettings.Models))]
			public partial class ChatModelSettings : LLMDesktopAssistant.NotifyPropertyChanged
			{
				private string _chatModel = string.Empty;

				[LLMDesktopAssistant.SourceGenerators.InheritedChatSetting]
				public string ChatModel
				{
					get => _chatModel;
					set => SetProperty(ref _chatModel, value);
				}
			}
		}

		namespace LLMDesktopAssistant.Settings.Application
		{
			public class ApplicationSettings : LLMDesktopAssistant.NotifyPropertyChanged
			{
				public LLMDesktopAssistant.LLM.Settings.ChatSettings InheritedChatSettings { get; set; } = new();
			}
		}
		""";

	private static (GeneratorDriverRunResult RunResult, Compilation OutputCompilation) RunGenerator(string source)
	{
		var compilation = CSharpCompilation.Create(
			"GeneratorTests",
			[CSharpSyntaxTree.ParseText(source)],
			_references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(new InheritedSettingsGenerator().AsSourceGenerator());
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

		return (driver.GetRunResult(), outputCompilation);
	}

	[Fact]
	public void GeneratesInheritanceMembers_ForAgentAndChatLevelSettings()
	{
		var (runResult, outputCompilation) = RunGenerator(SampleSource);

		var generated = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));

		// Agent-level setting: 3-level resolution
		Assert.Contains("SystemPromptInheritance", generated);
		Assert.Contains("GetEffectiveSystemPrompt", generated);
		Assert.Contains("SetEffectiveSystemPrompt", generated);
		Assert.Contains("chatSettings.InheritedAgentSettings.Prompts.SystemPrompt", generated);
		Assert.Contains("appSettings.InheritedChatSettings.InheritedAgentSettings.Prompts.SystemPrompt", generated);

		// Chat-level setting: 2-level resolution
		Assert.Contains("ChatModelInheritance", generated);
		Assert.Contains("GetEffectiveChatModel", generated);
		Assert.Contains("SetEffectiveChatModel", generated);
		Assert.Contains("appSettings.InheritedChatSettings.Models.ChatModel", generated);

		// Default levels
		Assert.Contains("ChatSettingsInheritanceLevel.Agent", generated); // agent setting default
		Assert.Contains("ChatSettingsInheritanceLevel.Profile", generated); // chat setting default

		var errors = outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
		Assert.True(errors.Count == 0, string.Join("\n", errors));
	}

	[Fact]
	public void ReportsDiagnostic_WhenClassIsNotPartial()
	{
		const string source = """
			namespace TestNamespace
			{
				[LLMDesktopAssistant.SourceGenerators.SettingsRoute("SomeRoute")]
				public class NotPartialSettings : LLMDesktopAssistant.NotifyPropertyChanged
				{
					[LLMDesktopAssistant.SourceGenerators.InheritedChatAgentSetting]
					public int Value { get; set; }
				}
			}
			""";

		var (runResult, _) = RunGenerator(source);

		Assert.Contains(runResult.Diagnostics, d => d.Id == "DASSGEN001");
		Assert.DoesNotContain(runResult.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Id != "DASSGEN001");
	}
}
