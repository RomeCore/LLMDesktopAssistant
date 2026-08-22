namespace LLMDesktopAssistant.Tools;

public static class ToolBehaviours
{
	public static ImmutableList<ToolBehaviour> AllValues { get; }

	public static ImmutableList<ToolBehaviour> AllValuesWithNone { get; }

	public static ImmutableList<ToolBehaviourCategory> AllCategories { get; }

	public static ImmutableList<ToolBehaviourCategory> AllCategoriesWithNone { get; }

	public static ImmutableDictionary<ToolBehaviourCategory, ImmutableList<ToolBehaviour>> ByCategory { get; }

	public static ImmutableDictionary<ToolBehaviourCategory, ToolBehaviour> ByCategoryFlags { get; }

	public static ImmutableList<ToolBehaviour> AllExceptSources { get; }

	static ToolBehaviours()
	{
		AllValues = Enum.GetValues<ToolBehaviour>()
			.Where(b => IsSingleFlag((ulong)b))
			.ToImmutableList();

		AllValuesWithNone = AllValues.Insert(0, ToolBehaviour.None);

		AllCategories = Enum.GetValues<ToolBehaviourCategory>()
			.Where(c => IsSingleFlag((ulong)c))
			.ToImmutableList();

		AllCategoriesWithNone = AllCategories.Insert(0, ToolBehaviourCategory.None);

		ByCategory = AllValues
			.GroupBy(ToolBehaviourCategoryClassifier.GetCategory)
			.ToImmutableDictionary(g => g.Key, g => g.ToImmutableList());

		ByCategoryFlags = AllValues
			.GroupBy(ToolBehaviourCategoryClassifier.GetCategory)
			.ToImmutableDictionary(g => g.Key, g => g.Aggregate(ToolBehaviour.None, (a, b) => a | b));

		AllExceptSources = AllValues
			.Where(b => ToolBehaviourCategoryClassifier.GetCategory(b) != ToolBehaviourCategory.Source)
			.ToImmutableList();
	}

	private static bool IsZeroFlag(ulong flag)
	{
		return flag == 0;
	}

	private static bool IsSingleFlag(ulong flag)
	{
		return flag != 0 && (flag & (flag - 1)) == 0;
	}

	private static bool IsMultipleFlag(ulong flag)
	{
		return (flag & (flag - 1)) != 0;
	}
}
