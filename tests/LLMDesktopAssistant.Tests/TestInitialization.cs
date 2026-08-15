using System.Runtime.CompilerServices;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tests;

internal static class TestInitialization
{
	[ModuleInitializer]
	internal static void Initialize()
	{
		ReflectionUtility.Initialize(AppDomain.CurrentDomain);
	}
}
