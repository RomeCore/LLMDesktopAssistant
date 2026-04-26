using LLMDesktopAssistant.Localization;
using Material.Icons;
using RCLargeLanguageModels.Tools;
using System.ComponentModel;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class RandomToolModule : ToolModule
	{
		public RandomToolModule()
		{
			AddTool(GenerateGUID,
				new ToolInitializationInfo
				{
					Name = "random-GUID",
					Description = "Generates a globally unique identifier (GUID).",
					Category = "random"
				});

			AddTool(GenerateRandomInteger,
				new ToolInitializationInfo
				{
					Name = "random-integer",
					Description = "Generates a random integer number.",
					Category = "random"
				});

			AddTool(GenerateRandomFloat,
				new ToolInitializationInfo
				{
					Name = "random-float",
					Description = "Generates a random floating-point number.",
					Category = "random"
				});

			AddTool(GenerateRandomDiceRoll,
				new ToolInitializationInfo
				{
					Name = "random-dice_roll",
					Description = "Simulates rolling dice (e.g., 2d6, 1d20).",
					Category = "random"
				});

			AddTool(GenerateRandomItemsFromList,
				new ToolInitializationInfo
				{
					Name = "random-items_from_list",
					Description = "Selects a random item from a provided list.",
					Category = "random"
				});

			AddTool(GenerateRandomShuffledList,
				new ToolInitializationInfo
				{
					Name = "random-shuffle_list",
					Description = "Returns a shuffled version of a provided list.",
					Category = "random"
				});
		}

		private ReactiveToolResult GenerateGUID()
		{
			var value = Guid.NewGuid();
			return new ReactiveToolResult
			{
				ResultContent = value.ToString(),
				StatusIcon = MaterialIconKind.IdCard,
				StatusTitle = value.ToString()
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult GenerateRandomInteger([Description("The minimum inclusive value")] long minValue,
			[Description("The maximum inclusive value")] long maxValue)
		{
			var value = Random.Shared.NextInt64(minValue, maxValue + 1);
			return new ReactiveToolResult
			{
				ResultContent = value.ToString(),
				StatusIcon = MaterialIconKind.DiceMultipleOutline,
				StatusTitle = value.ToString()
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult GenerateRandomFloat([Description("The minimum inclusive value")] double minValue,
			[Description("The maximum inclusive value")] double maxValue)
		{
			var range = maxValue - minValue;
			var value = Random.Shared.NextDouble() * range + minValue;
			return new ReactiveToolResult
			{
				ResultContent = value.ToString(),
				StatusIcon = MaterialIconKind.DiceMultipleOutline,
				StatusTitle = value.ToString()
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult GenerateRandomDiceRoll(
			[Description("Count of dices to roll")] int numberOfDice = 1,
			[Description("Number of sides per die")] int sides = 6)
		{
			var rolls = new List<int>();
			for (int i = 0; i < numberOfDice; i++)
			{
				rolls.Add(Random.Shared.Next(1, sides + 1));
			}

			var total = rolls.Sum();
			var value = $"{total} (Rolls: {string.Join(", ", rolls)})";

			return new ReactiveToolResult
			{
				ResultContent = value,
				StatusIcon = sides switch
				{
					4 => MaterialIconKind.DiceD4,
					6 => MaterialIconKind.DiceD6,
					8 => MaterialIconKind.DiceD8,
					10 => MaterialIconKind.DiceD10,
					12 => MaterialIconKind.DiceD12,
					20 => MaterialIconKind.DiceD20,
					_ => MaterialIconKind.DiceMultiple
				},
				StatusTitle = $"{LocalizationManager.LocalizeStatic("dice_rolled")} {numberOfDice}d{sides}: {string.Join(", ", rolls)}"
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult GenerateRandomItemsFromList(
			[Description("List of items")] string[] items,
			[Description("Number of items to select")] int count)
		{
			var values = Random.Shared.GetItems(items, count);
			var value = string.Join(", ", values);

			return new ReactiveToolResult
			{
				ResultContent = value,
				StatusIcon = MaterialIconKind.Shuffle,
				StatusTitle = value
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult GenerateRandomShuffledList(
			[Description("List of items to shuffle")] string[] items)
		{
			Random.Shared.Shuffle(items);
			var value = string.Join(", ", items);

			return new ReactiveToolResult
			{
				ResultContent = value,
				StatusIcon = MaterialIconKind.Shuffle,
				StatusTitle = string.Format(LocalizationManager.LocalizeStatic("list_shuffled"), items.Length)
			}.CompleteWithSuccess();
		}
	}
}