using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools.Forms;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Tools;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.Tools.Implementations;

/// <summary>
/// Module for user interaction tools via forms (Human-in-the-Loop).
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
				Description = "Requests the user to confirm an action. " +
					"Shows a message with 'Confirm' and 'Cancel' buttons. " +
					"Use this tool when you need to ask the user for permission before performing an important or dangerous action.",
				Category = "forms",
				AskForConfirmation = false
			});

		AddTool(FormsChoice,
			new ToolInitializationInfo
			{
				Name = "forms-choice",
				Description = "Offers the user to choose one or more options from a list. " +
					"Can allow custom input (allowCustom). " +
					"Use when the user needs to make a selection from the provided options.",
				Category = "forms",
				AskForConfirmation = false
			});

		AddTool(FormsInput,
			new ToolInitializationInfo
			{
				Name = "forms-input",
				Description = "Requests data input from the user via a form with one or more fields. " +
					"Supports field types: text, number, password, multiline. " +
					"Use when you need structured data from the user.",
				Category = "forms",
				AskForConfirmation = false
			});

		AddTool(FormsFilePicker,
			new ToolInitializationInfo
			{
				Name = "forms-file_picker",
				Description = "Opens a file selection dialog for the user. " +
					"Can filter by extensions and allow multiple file selection. " +
					"Use when the user needs to specify a file path on their system.",
				Category = "forms",
				AskForConfirmation = false
			});
	}

	public async Task<ReactiveToolResult> FormsConfirm(
		[Description("Title of the confirmation question. For example: 'Delete file?', 'Confirm sending?'")] string title,
		[Description("Detailed description of what needs to be confirmed. Provide context for the user.")] string? description,
		[Description("Text on the confirm button (default: 'OK')")] string? confirmText,
		[Description("Text on the cancel button (default: 'Cancel')")] string? cancelText,
		[Description("Is this a dangerous action? If true, the button will be red (default: false)")] bool? isDanger,
		ToolExecutionContext context,
		CancellationToken cancellationToken = default)
	{
		var message = context.Message;
		var viewModel = new FormsConfirmViewModel
		{
			Title = title,
			Description = description ?? string.Empty,
			ConfirmText = confirmText ?? "OK",
			CancelText = cancelText ?? "Cancel",
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
			return ReactiveToolResult.CreateError("User cancelled the operation or the request was interrupted.");
		}

		if (confirmed)
			return ReactiveToolResult.CreateSuccess($"User confirmed: \"{title}\".");
		else
			return ReactiveToolResult.CreateSuccess($"User declined: \"{title}\".");
	}

	public async Task<ReactiveToolResult> FormsChoice(
		ToolExecutionContext context,
		[Description("Title of the question")] string title,
		[Description("Detailed description of what needs to be selected")] string? description,
		[Description("Array of options to choose from. Each option is a string that will be shown to the user and returned as a value.")]
		string[] options,
		[Description("Can multiple options be selected (default: false)")] bool? allowMultiple,
		[Description("Can the user enter a custom option (default: false)")] bool? allowCustom,
		[Description("Minimum number of selectable options (default: 1)")] int? minSelect,
		[Description("Maximum number of selectable options (default: 1, when allowMultiple=true: all options)")] int? maxSelect,
		CancellationToken cancellationToken = default)
	{
		var message = context.Message;
		var formOptions = new List<ChoiceOption>();

		foreach (var option in options)
		{
			formOptions.Add(new ChoiceOption
			{
				Value = option,
				Label = option
			});
		}

		var viewModel = new FormsChoiceViewModel(formOptions)
		{
			Title = title,
			Description = description ?? string.Empty,
			AllowMultiple = allowMultiple ?? false,
			AllowCustom = allowCustom ?? false,
			MinSelect = minSelect ?? 1,
			MaxSelect = maxSelect ?? (allowMultiple == true ? options.Length : 1)
		};

		message.AdditionalViewModels.Add(viewModel);

		ChoiceResult result;
		try
		{
			result = await viewModel.Result.WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			message.AdditionalViewModels.Remove(viewModel);
			return ReactiveToolResult.CreateError("User cancelled the selection.");
		}

		var selectedStr = string.Join(", ", result.Selected);
		var resultText = $"User selected: {selectedStr}.";
		if (!string.IsNullOrWhiteSpace(result.Custom))
			resultText += $" Additional text: \"{result.Custom}\".";

		return ReactiveToolResult.CreateSuccess(resultText);
	}

	public async Task<ReactiveToolResult> FormsInput(
		ToolExecutionContext context,
		[Description("Title of the form")] string title,
		[Description("Description of the form")] string? description,
		[Description("Array of JSON objects describing form fields. Each object must contain:\n- id (string, required) — field key in the result\n- label (string, required) — display label for the field\n- type (string, optional) — field type: 'text' (default), 'number', 'password', 'multiline'\n- placeholder (string, optional) — placeholder text inside the field\n- required (bool, optional) — whether the field is required (default: false)\n- default (string, optional) — default value\nExample: [{\"id\": \"name\", \"label\": \"Name\", \"required\": true}, {\"id\": \"comment\", \"label\": \"Comment\", \"type\": \"multiline\"}]")]
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
			return ReactiveToolResult.CreateError("Failed to parse form fields. Make sure a valid array of fields with id and label is provided.");

		var message = context.Message;
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
			return ReactiveToolResult.CreateError("User cancelled data input.");
		}

		var valuesStr = string.Join(", ", result.Values.Select(kv => $"{kv.Key}=\"{kv.Value}\""));
		return ReactiveToolResult.CreateSuccess($"User entered data: {valuesStr}.");
	}

	public async Task<ReactiveToolResult> FormsFilePicker(
		ToolExecutionContext context,
		[Description("Title of the file selection dialog")] string title,
		[Description("Description or instructions for the user")] string? description,
		[Description("The mode of the dialog."), Enum(["open", "save", "directory"])] string mode,
		[Description("Extension filter, e.g. '*.cs;*.py;*.js'")] string? filter = null,
		[Description("Allow multiple file selection")] bool allowMultiple = false,
		CancellationToken cancellationToken = default)
	{
		var message = context.Message;
		var viewModel = new FormsFilePickerViewModel
		{
			Title = title,
			Description = description ?? string.Empty,
			Mode = mode switch
			{
				"open" => FilePickerMode.Open,
				"save" => FilePickerMode.Save,
				"directory" => FilePickerMode.Directory,
				_ => throw new ArgumentException("Invalid mode specified for file picker.", nameof(mode))
			},
			Filter = filter,
			AllowMultiple = allowMultiple
		};

		message.AdditionalViewModels.Add(viewModel);

		var result = await viewModel.Result.WaitAsync(cancellationToken);
		if (result.Paths.Length == 0)
			return ReactiveToolResult.CreateError("User did not select any files.");

		var pathsStr = string.Join(", ", result.Paths);
		return ReactiveToolResult.CreateSuccess($"User selected files: {pathsStr}.");
	}
}
