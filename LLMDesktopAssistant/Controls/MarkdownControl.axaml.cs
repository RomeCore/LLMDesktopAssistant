using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Controls;

public partial class MarkdownControl : UserControl
{
	public static readonly StyledProperty<string> MarkdownTextProperty =
		AvaloniaProperty.Register<MarkdownControl, string>(
			nameof(MarkdownText));

	public static readonly StyledProperty<bool> UsePlaintextProperty =
		AvaloniaProperty.Register<MarkdownControl, bool>(
			nameof(UsePlaintext));

	public string MarkdownText
	{
		get => GetValue(MarkdownTextProperty);
		set => SetValue(MarkdownTextProperty, value);
	}

	public bool UsePlaintext
	{
		get => GetValue(UsePlaintextProperty);
		set => SetValue(UsePlaintextProperty, value);
	}

	static MarkdownControl()
	{
		MarkdownTextProperty.Changed.AddClassHandler<MarkdownControl>((o, e) => o.MarkdownTextChanged(e.NewValue as string, o.UsePlaintext));
		UsePlaintextProperty.Changed.AddClassHandler<MarkdownControl>((o, e) => o.MarkdownTextChanged(o.MarkdownText, (bool)e.NewValue!));
	}

	private readonly ObservableStringBuilder _markdownBuilder = new();

	public MarkdownControl()
	{
		InitializeComponent();

		MarkdownRenderer.LinkClick += MarkdownRenderer_LinkClick;
		MarkdownRenderer.ImageBasePath = null;
		MarkdownRenderer.CodeBlockColorTheme = TextMateSharp.Grammars.ThemeName.Monokai;
		MarkdownRenderer.MarkdownBuilder = _markdownBuilder;
	}

	private void MarkdownTextChanged(string? newText, bool usePlaintext)
	{
		newText ??= string.Empty;

		if (usePlaintext)
		{
			_markdownBuilder.Clear();
			MarkdownTextBlock.Inlines = [new Run(newText)];
		}
		else
		{
			MarkdownTextBlock.Inlines?.Clear();
			var oldText = _markdownBuilder.ToString();
			if (!newText.StartsWith(oldText))
				_markdownBuilder.Clear();
			string delta = newText[_markdownBuilder.Length..];
			if (!string.IsNullOrEmpty(delta))
				_markdownBuilder.Append(delta);
		}
	}

	private void MarkdownRenderer_LinkClick(object? sender, LinkClickedEventArgs e)
	{
		if (e.HRef != null)
		{
			ServiceRegistry.Provider.GetService<ILinkOpener>()?.OpenLink(e.HRef);
		}
	}

	// The pizdec

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		var chatView = this.FindParent<ChatView>();
		var chatViewModel = chatView?.DataContext as ChatViewModel;
		var chat = chatViewModel?.Chat;

		chat?.PropertyChanged += Chat_PropertyChanged;
		MarkdownRenderer.ImageBasePath = chat?.Settings?.Environment?.GetWorkingDirectory();
	}

	private WeakReference<ChatSettings>? _chatSettingsWeakRef;
	private WeakReference<ChatEnvironmentSettings>? _envSettingsWeakRef;

	private void Chat_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(Chat.Settings))
			return;

		if (_chatSettingsWeakRef?.TryGetTarget(out var oldChatSettings) ?? false)
			oldChatSettings.PropertyChanged -= ChatSettings_PropertyChanged;
		var chat = (Chat)sender!;
		_chatSettingsWeakRef = null;
		if (chat.Settings != null)
		{
			_chatSettingsWeakRef = new(chat.Settings);
			chat.Settings.PropertyChanged += ChatSettings_PropertyChanged;
		}
		MarkdownRenderer.ImageBasePath = chat?.Settings?.Environment?.GetWorkingDirectory();
	}

	private void ChatSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ChatSettings.Environment))
			return;

		if (_envSettingsWeakRef?.TryGetTarget(out var oldEnvSettings) ?? false)
			oldEnvSettings.PropertyChanged -= EnvSettings_PropertyChanged;
		var chatSettings = (ChatSettings)sender!;
		_envSettingsWeakRef = null;
		if (chatSettings.Environment != null)
		{
			_envSettingsWeakRef = new(chatSettings.Environment);
			chatSettings.Environment.PropertyChanged += EnvSettings_PropertyChanged;
		}
		MarkdownRenderer.ImageBasePath = chatSettings.Environment?.GetWorkingDirectory();
	}

	private void EnvSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ChatEnvironmentSettings.WorkingDirectory))
			return;

		var envSettings = (ChatEnvironmentSettings)sender!;
		MarkdownRenderer.ImageBasePath = envSettings.GetWorkingDirectory();
	}
}