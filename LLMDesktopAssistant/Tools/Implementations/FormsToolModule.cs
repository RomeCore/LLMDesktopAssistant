using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools.Forms;
using RCLargeLanguageModels.Tools;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.Tools.Implementations;

/// <summary>
/// Модуль инструментов для взаимодействия с пользователем через формы (Human-in-the-Loop).
/// </summary>
[ToolModule]
public class FormsToolModule : ToolModule
{
	public FormsToolModule()
	{
		AddTool(FormsConfirm,
			new ToolInitializationInfo
			{
				Name = "forms-confirm",
				Description = "Запрашивает у пользователя подтверждение действия. " +
					"Показывает сообщение с кнопками 'Подтвердить' и 'Отмена'. " +
					"Используй этот инструмент когда нужно спросить разрешение пользователя перед выполнением важного или опасного действия.",
				Category = "forms",
				AskForConfirmation = false
			});

		AddTool(FormsChoice,
			new ToolInitializationInfo
			{
				Name = "forms-choice",
				Description = "Предлагает пользователю выбрать один или несколько вариантов из списка. " +
					"Может разрешить ввод своего варианта (allowCustom). " +
					"Используй когда нужно, чтобы пользователь сделал выбор из предложенных опций.",
				Category = "forms",
				AskForConfirmation = false
			});

		AddTool(FormsInput,
			new ToolInitializationInfo
			{
				Name = "forms-input",
				Description = "Запрашивает у пользователя ввод данных через форму с одним или несколькими полями. " +
					"Поддерживает поля разных типов: text, number, password, multiline. " +
					"Используй когда нужно получить от пользователя структурированные данные.",
				Category = "forms",
				AskForConfirmation = false
			});

		AddTool(FormsFilePicker,
			new ToolInitializationInfo
			{
				Name = "forms-file_picker",
				Description = "Открывает диалог выбора файла для пользователя. " +
					"Может фильтровать по расширениям и разрешать выбор нескольких файлов. " +
					"Используй когда нужно, чтобы пользователь указал путь к файлу на своей системе.",
				Category = "forms",
				AskForConfirmation = false
			});
	}

	/// <summary>
	/// Возвращает AssistantMessage из контекста выполнения.
	/// </summary>
	private static AssistantMessage GetMessage(ToolExecutionContext context)
	{
		return context.Message;
	}

	public async Task<ReactiveToolResult> FormsConfirm(
		[Description("Заголовок вопроса/подтверждения. Например: 'Удалить файл?', 'Подтвердить отправку?'")] string title,
		[Description("Подробное описание того, что нужно подтвердить. Уточни контекст для пользователя.")] string? description,
		[Description("Текст на кнопке подтверждения (по умолчанию 'OK')")] string? confirmText,
		[Description("Текст на кнопке отмены (по умолчанию 'Отмена')")] string? cancelText,
		[Description("Опасное ли действие? Если true, кнопка будет красной (по умолчанию false)")] bool? isDanger,
		ToolExecutionContext context,
		CancellationToken cancellationToken = default)
	{
		var message = GetMessage(context);
		var viewModel = new FormsConfirmViewModel
		{
			Title = title,
			Description = description ?? string.Empty,
			ConfirmText = confirmText ?? "OK",
			CancelText = cancelText ?? "Отмена",
			IsDanger = isDanger ?? false
		};

		message.AdditionalViewModels.Add(viewModel);

		bool confirmed;
		try
		{
			confirmed = await viewModel.Result.WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			message.AdditionalViewModels.Remove(viewModel);
			return ReactiveToolResult.CreateError("Пользователь отменил операцию или запрос был прерван.");
		}

		if (confirmed)
			return ReactiveToolResult.CreateSuccess($"Пользователь подтвердил: \"{title}\".");
		else
			return ReactiveToolResult.CreateSuccess($"Пользователь отказался: \"{title}\".");
	}

	public async Task<ReactiveToolResult> FormsChoice(
		ToolExecutionContext context,
		[Description("Заголовок вопроса")] string title,
		[Description("Подробное описание того, что нужно выбрать")] string? description,
		[Description("Массив вариантов для выбора. Каждый вариант — строка, которая будет показана пользователю и возвращена как значение.")]
		string[] options,
		[Description("Можно ли выбрать несколько вариантов (по умолчанию false)")] bool? allowMultiple,
		[Description("Можно ли ввести свой вариант (по умолчанию false)")] bool? allowCustom,
		[Description("Минимальное количество выбираемых вариантов (по умолчанию 1)")] int? minSelect,
		[Description("Максимальное количество выбираемых вариантов (по умолчанию 1, при allowMultiple=true по умолчанию все варианты)")] int? maxSelect,
		CancellationToken cancellationToken = default)
	{
		var message = GetMessage(context);
		var viewModel = new FormsChoiceViewModel
		{
			Title = title,
			Description = description ?? string.Empty,
			AllowMultiple = allowMultiple ?? false,
			AllowCustom = allowCustom ?? false,
			MinSelect = minSelect ?? 1,
			MaxSelect = maxSelect ?? (allowMultiple == true ? options.Length : 1)
		};

		foreach (var option in options)
		{
			viewModel.Options.Add(new ChoiceOption
			{
				Value = option,
				Label = option
			});
		}

		message.AdditionalViewModels.Add(viewModel);

		ChoiceResult result;
		try
		{
			result = await viewModel.Result.WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			message.AdditionalViewModels.Remove(viewModel);
			return ReactiveToolResult.CreateError("Пользователь отменил выбор.");
		}

		var selectedStr = string.Join(", ", result.Selected);
		var resultText = $"Пользователь выбрал: {selectedStr}.";
		if (!string.IsNullOrWhiteSpace(result.Custom))
			resultText += $" Дополнительный текст: \"{result.Custom}\".";

		return ReactiveToolResult.CreateSuccess(resultText);
	}

	public async Task<ReactiveToolResult> FormsInput(
		ToolExecutionContext context,
		[Description("Заголовок формы")] string title,
		[Description("Описание формы")] string? description,
		[Description("Массив JSON-объектов, описывающих поля формы. Каждый объект должен содержать:\n- id (string, обязательный) — ключ поля в результате\n- label (string, обязательный) — отображаемая подпись поля\n- type (string, опционально) — тип поля: 'text' (по умолчанию), 'number', 'password', 'multiline'\n- placeholder (string, опционально) — подсказка внутри поля\n- required (bool, опционально) — обязательно ли поле (по умолчанию false)\n- default (string, опционально) — значение по умолчанию\nПример: [{\"id\": \"name\", \"label\": \"Имя\", \"required\": true}, {\"id\": \"comment\", \"label\": \"Комментарий\", \"type\": \"multiline\"}]")]
		JsonNode[] fields,
		CancellationToken cancellationToken = default)
	{
		var formFields = new List<InputField>();

		foreach (var fieldNode in fields)
		{
			if (fieldNode is not JsonObject fieldObj)
				continue;

			var id = fieldObj["id"]?.GetValue<string>() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(id))
				continue;

			formFields.Add(new InputField
			{
				Id = id,
				Label = fieldObj["label"]?.GetValue<string>() ?? id,
				Placeholder = fieldObj["placeholder"]?.GetValue<string>() ?? string.Empty,
				FieldType = fieldObj["type"]?.GetValue<string>() ?? "text",
				IsRequired = fieldObj["required"]?.GetValue<bool>() ?? false,
				Value = fieldObj["default"]?.GetValue<string>() ?? string.Empty
			});
		}

		if (formFields.Count == 0)
			return ReactiveToolResult.CreateError("Не удалось распарсить поля формы. Убедитесь, что передан корректный массив полей с id и label.");

		var message = GetMessage(context);
		var viewModel = new FormsInputViewModel(formFields)
		{
			Title = title,
			Description = description ?? string.Empty
		};

		message.AdditionalViewModels.Add(viewModel);

		InputResult result;
		try
		{
			result = await viewModel.Result.WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			message.AdditionalViewModels.Remove(viewModel);
			return ReactiveToolResult.CreateError("Пользователь отменил ввод данных.");
		}

		var valuesStr = string.Join(", ", result.Values.Select(kv => $"{kv.Key}=\"{kv.Value}\""));
		return ReactiveToolResult.CreateSuccess($"Пользователь ввёл данные: {valuesStr}.");
	}

	public async Task<ReactiveToolResult> FormsFilePicker(
		ToolExecutionContext context,
		[Description("Заголовок диалога выбора файла")] string title,
		[Description("Описание или инструкция для пользователя")] string? description,
		[Description("Фильтр расширений, например '*.cs;*.py;*.js'")] string? filter,
		[Description("Разрешить выбор нескольких файлов (по умолчанию false)")] bool? allowMultiple,
		CancellationToken cancellationToken = default)
	{
		var message = GetMessage(context);
		var viewModel = new FormsFilePickerViewModel
		{
			Title = title,
			Description = description ?? string.Empty,
			Filter = filter,
			AllowMultiple = allowMultiple ?? false
		};

		message.AdditionalViewModels.Add(viewModel);

		FilePickerResult result;
		try
		{
			result = await viewModel.Result.WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			message.AdditionalViewModels.Remove(viewModel);
			return ReactiveToolResult.CreateError("Пользователь отменил выбор файла.");
		}

		if (result.Paths.Length == 0)
			return ReactiveToolResult.CreateError("Пользователь не выбрал ни одного файла.");

		var pathsStr = string.Join(", ", result.Paths);
		return ReactiveToolResult.CreateSuccess($"Пользователь выбрал файлы: {pathsStr}.");
	}
}
