using LLMDesktopAssistant.LLM.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.LLM.MVVM.ContextTabs
{
	public class ChatContextTabViewModel : NotifyPropertyChanged
	{
		private Guid _guid = Guid.NewGuid();
		/// <summary>
		/// Gets or sets the GUID for this chat tab view model, used for persistence, especially for removing it from the database.
		/// Do not change this GUID by itself.
		/// </summary>
		public Guid Guid
		{
			get => _guid;
			set => SetProperty(ref _guid, value);
		}

		private string? _title;
		/// <summary>
		/// Gets or sets the title of this chat tab view model. This is used for displaying the tab in the UI.
		/// </summary>
		public string? Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}

		private bool _isVisible = true;
		/// <summary>
		/// Gets or sets a value indicating whether the chat tab view model is visible.
		/// Tip: view model can be invisible when used only to store data that is not displayed in the UI.
		/// </summary>
		public bool IsVisible
		{
			get => _isVisible;
			set => SetProperty(ref _isVisible, value);
		}

		private bool _isTemporary = false;
		/// <summary>
		/// Gets or sets a value indicating whether the chat tab view model is temporary.
		/// Temporary view models are not stored in the database, and are only used for short-lived operations.
		/// </summary>
		public bool IsTemporary
		{
			get => _isTemporary;
			set => SetProperty(ref _isTemporary, value);
		}
	}
}
