using System.ComponentModel;
using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule(chatScoped: false)]
	public class RandomToolModule : ToolModule
	{
		private Random _random = Random.Shared;

		public RandomToolModule()
		{
			AddTool(GenerateGUID,
				new ToolInitializationInfo
				{
					Name = "random-GUID",
					Description = "Generates a globally unique identifier (GUID).",
					TitleKey = Locale.GetKey("tool.name.random-GUID"),
					DescriptionKey = Locale.GetKey("tool.description.random-GUID"),
					CategoryKey = Locale.GetKey("tool.category.random")
				});

			AddTool(GenerateRandomInteger,
				new ToolInitializationInfo
				{
					Name = "random-integer",
					Description = "Generates a random integer number.",
					TitleKey = Locale.GetKey("tool.name.random-integer"),
					DescriptionKey = Locale.GetKey("tool.description.random-integer"),
					CategoryKey = Locale.GetKey("tool.category.random")
				});

			AddTool(GenerateRandomFloat,
				new ToolInitializationInfo
				{
					Name = "random-float",
					Description = "Generates a random floating-point number.",
					TitleKey = Locale.GetKey("tool.name.random-float"),
					DescriptionKey = Locale.GetKey("tool.description.random-float"),
					CategoryKey = Locale.GetKey("tool.category.random")
				});

			AddTool(GenerateCoinFlip,
				new ToolInitializationInfo
				{
					Name = "random-coin_flip",
					Description = "Simulates flipping a coin. Returns 'Heads' or 'Tails'.",
					TitleKey = Locale.GetKey("tool.name.random-coin_flip"),
					DescriptionKey = Locale.GetKey("tool.description.random-coin_flip"),
					CategoryKey = Locale.GetKey("tool.category.random")
				});

			AddTool(GenerateCheckChance,
				new ToolInitializationInfo
				{
					Name = "random-check_chance",
					Description = "Simulates a chance check with a given probability. Returns 'Success' or 'Failure'.",
					TitleKey = Locale.GetKey("tool.name.random-check_chance"),
					DescriptionKey = Locale.GetKey("tool.description.random-check_chance"),
					CategoryKey = Locale.GetKey("tool.category.random")
				});

			AddTool(GenerateRandomDiceRoll,
				new ToolInitializationInfo
				{
					Name = "random-dice_roll",
					Description = "Simulates rolling dice (e.g., 2d6, 1d20).",
					TitleKey = Locale.GetKey("tool.name.random-dice_roll"),
					DescriptionKey = Locale.GetKey("tool.description.random-dice_roll"),
					CategoryKey = Locale.GetKey("tool.category.random")
				});

			AddTool(GenerateRandomItemsFromList,
				new ToolInitializationInfo
				{
					Name = "random-items_from_list",
					Description = "Selects a random item from a provided list.",
					TitleKey = Locale.GetKey("tool.name.random-items_from_list"),
					DescriptionKey = Locale.GetKey("tool.description.random-items_from_list"),
					CategoryKey = Locale.GetKey("tool.category.random")
				});

			AddTool(GenerateRandomShuffledList,
				new ToolInitializationInfo
				{
					Name = "random-shuffle_list",
					Description = "Returns a shuffled version of a provided list.",
					TitleKey = Locale.GetKey("tool.name.random-shuffle_list"),
					DescriptionKey = Locale.GetKey("tool.description.random-shuffle_list"),
					CategoryKey = Locale.GetKey("tool.category.random")
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

		private ReactiveToolResult GenerateRandomInteger(
			[Description("The minimum inclusive value")] long minValue,
			[Description("The maximum inclusive value")] long maxValue)
		{
			var value = _random.NextInt64(minValue, maxValue + 1);
			return new ReactiveToolResult
			{
				ResultContent = value.ToString(),
				StatusIcon = MaterialIconKind.DiceMultipleOutline,
				StatusTitle = value.ToString()
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult GenerateRandomFloat(
			[Description("The minimum inclusive value")] double minValue,
			[Description("The maximum inclusive value")] double maxValue)
		{
			var range = maxValue - minValue;
			var value = _random.NextDouble() * range + minValue;
			return new ReactiveToolResult
			{
				ResultContent = value.ToString(),
				StatusIcon = MaterialIconKind.DiceMultipleOutline,
				StatusTitle = value.ToString()
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult GenerateCoinFlip()
		{
			bool result = _random.NextDouble() < 0.5;
			var value = result ? "Heads" : "Tails";

			return new ReactiveToolResult
			{
				ResultContent = value,
				StatusIcon = MaterialIconKind.Coins,
				StatusTitle = result ? LocalizationManager.LocalizeStatic("coin_heads") : LocalizationManager.LocalizeStatic("coin_tails")
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult GenerateCheckChance(
			[Description("The probability of success as a percentage from 0 to 100")] double chance)
		{
			var gained = _random.NextDouble() * 100;
			var result = gained <= chance;
			var value = $"{gained} < {chance}? " + (result ? "Success" : "Failure");

			return new ReactiveToolResult
			{
				ResultContent = value,
				StatusIcon = result ? MaterialIconKind.Check : MaterialIconKind.Close,
				StatusTitle = $"{chance}%: " + (result ? LocalizationManager.LocalizeStatic("check_success") : LocalizationManager.LocalizeStatic("check_failure"))
			}.CompleteWithSuccess();
		}

		private ReactiveToolResult GenerateRandomDiceRoll(
			[Description("Count of dices to roll")] int numberOfDice = 1,
			[Description("Number of sides per die")] int sides = 6)
		{
			var rolls = new List<int>();
			for (int i = 0; i < numberOfDice; i++)
			{
				rolls.Add(_random.Next(1, sides + 1));
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
			[Description("Number of items to select")] int count = 1)
		{
			var values = _random.GetItems(items, count);
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
			_random.Shuffle(items);
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