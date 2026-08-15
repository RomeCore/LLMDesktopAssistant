using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tests;

public class LocFileParserTests
{
	[Fact]
	public void Parse_SingleLineEntriesAndMetadata()
	{
		var content = """"
			%locale: ru-RU
			%namespace: ui.settings.tool_behaviour

			fileread: Чтение файлов
			filewrite: Запись файлов
			"""";

		var document = LocFileParser.Parse(content);

		Assert.Equal("ru-RU", document.Locale);
		Assert.Equal("ui.settings.tool_behaviour", document.Namespace);
		Assert.Equal(2, document.Entries.Count);
		Assert.Equal("Чтение файлов", document.Entries["ui.settings.tool_behaviour.fileread"]);
		Assert.Equal("Запись файлов", document.Entries["ui.settings.tool_behaviour.filewrite"]);
	}

	[Fact]
	public void Parse_EntriesWithoutNamespaceUseFullKeys()
	{
		var content = """"
			%locale: ru-RU

			ui.common.save: Сохранить
			"""";

		var document = LocFileParser.Parse(content);

		Assert.Equal("ru-RU", document.Locale);
		Assert.Null(document.Namespace);
		Assert.Equal("Сохранить", document.Entries["ui.common.save"]);
	}

	[Fact]
	public void Parse_MultilineValue()
	{
		var content = """"
			%locale: ru-RU

			ui.about.description """
			Первая строка
			    Вторая строка с отступом
			"""
			"""";

		var document = LocFileParser.Parse(content);

		Assert.Equal("Первая строка\n    Вторая строка с отступом", document.Entries["ui.about.description"]);
	}

	[Fact]
	public void Parse_MultilineValueWithCommonIndent()
	{
		var content = """"
			%locale: ru-RU
			ui.about.description """
			    Строка один
			    Строка два
			"""
			"""";

		var document = LocFileParser.Parse(content);

		Assert.Equal("Строка один\nСтрока два", document.Entries["ui.about.description"]);
	}

	[Fact]
	public void Parse_EmptyMultilineValue()
	{
		var content = """"
			%locale: ru-RU
			ui.empty """
			"""
			"""";

		var document = LocFileParser.Parse(content);

		Assert.Equal(string.Empty, document.Entries["ui.empty"]);
	}

	[Fact]
	public void Parse_CommentsAndEmptyLinesAreIgnored()
	{
		var content = """"
			// A comment before metadata
			%locale: ru-RU

			// Another comment

			ui.common.save: Сохранить
			"""";

		var document = LocFileParser.Parse(content);

		Assert.Equal("ru-RU", document.Locale);
		Assert.Single(document.Entries);
		Assert.Equal("Сохранить", document.Entries["ui.common.save"]);
	}

	[Fact]
	public void Parse_InlineValueKeepsEverythingUntilEndOfLine()
	{
		var content = """"
			%locale: ru-RU
			ui.common.save: Сохранить // not a comment
			"""";

		var document = LocFileParser.Parse(content);

		Assert.Equal("Сохранить // not a comment", document.Entries["ui.common.save"]);
	}

	[Fact]
	public void Parse_MissingLocaleThrows()
	{
		var content = """"
			%namespace: ui.common
			save: Save
			"""";

		Assert.Throws<LocFileParseException>(() => LocFileParser.Parse(content));
	}

	[Fact]
	public void Parse_DuplicateKeyThrows()
	{
		var content = """"
			%locale: ru-RU
			%namespace: ui.common

			save: Сохранить
			save: Сохранить ещё раз
			"""";

		Assert.Throws<LocFileParseException>(() => LocFileParser.Parse(content));
	}

	[Fact]
	public void Parse_GarbageLineThrows()
	{
		var content = """"
			%locale: ru-RU
			this is not a valid entry
			"""";

		Assert.Throws<LocFileParseException>(() => LocFileParser.Parse(content));
	}

	[Fact]
	public void Parse_NamespaceAfterEntriesIsAppliedToAllEntries()
	{
		var content = """"
			%locale: ru-RU

			save: Сохранить

			%namespace: ui.common
			"""";

		var document = LocFileParser.Parse(content);

		Assert.Equal("ui.common.save", document.Entries.Keys.Single());
	}
}
