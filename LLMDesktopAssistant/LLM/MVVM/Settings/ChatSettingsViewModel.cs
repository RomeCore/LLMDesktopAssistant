using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.MCP;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.ToolModules;
using MaterialDesignThemes.Wpf;
using ModelContextProtocol.Server;
using RCLargeLanguageModels.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LLMDesktopAssistant.LLM.MVVM.Settings
{
	[ViewModelFor(typeof(ChatSettingsView))]
	public class ChatSettingsViewModel : ViewModelBase
	{
		public ChatSettings Settings { get; }
		public Chat Chat { get; }

		public ChatGeneralSettingsViewModel GeneralSettings { get; }
		public ChatToolSettingsViewModel ToolSettings { get; }
		public ChatMCPSettingsViewModel McpSettings { get; }

		public ChatSettingsViewModel(ChatSettings settings, Chat chat)
		{
			Settings = settings;
			Chat = chat;

			GeneralSettings = new ChatGeneralSettingsViewModel(this);
			ToolSettings = new ChatToolSettingsViewModel(this);
			McpSettings = new ChatMCPSettingsViewModel(this);
		}
	}
}