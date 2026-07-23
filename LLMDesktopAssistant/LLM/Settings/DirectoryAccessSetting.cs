using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents a setting for accessing directories.
	/// </summary>
	public class DirectoryAccessSetting : NotifyPropertyChanged
	{
		private string? _path;
		/// <summary>
		/// The path to the directory that is being accessed.
		/// </summary>
		public string? Path
		{
			get => _path;
			set => SetProperty(ref _path, value);
		}

		private DirectoryAccessMode _accessMode;
		/// <summary>
		/// Gets or sets the access mode for this directory.
		/// </summary>
		public DirectoryAccessMode AccessMode
		{
			get => _accessMode;
			set
			{
				if (SetProperty(ref _accessMode, value))
				{
					RaisePropertyChanged(nameof(IsRead));
					RaisePropertyChanged(nameof(IsWrite));
					RaisePropertyChanged(nameof(IsExecute));
					RaisePropertyChanged(nameof(IsAccessDenied));
				}
			}
		}

		private bool _isEnabled = true;
		/// <summary>
		/// Whether the working directory access rule is enabled or not. Used for convenience to disable certain settings without removing them.
		/// </summary>
		public bool IsEnabled
		{
			get => _isEnabled;
			set => SetProperty(ref _isEnabled, value);
		}

		/// <summary>
		/// Whether read access is granted.
		/// </summary>
		[JsonIgnore]
		public bool IsRead
		{
			get => (_accessMode & DirectoryAccessMode.Read) == DirectoryAccessMode.Read;
			set
			{
				var newMode = value ? _accessMode | DirectoryAccessMode.Read : _accessMode & ~DirectoryAccessMode.Read;
				if (newMode != _accessMode)
					AccessMode = newMode;
			}
		}

		/// <summary>
		/// Whether write access is granted.
		/// </summary>
		[JsonIgnore]
		public bool IsWrite
		{
			get => (_accessMode & DirectoryAccessMode.Write) == DirectoryAccessMode.Write;
			set
			{
				var newMode = value ? _accessMode | DirectoryAccessMode.Write : _accessMode & ~DirectoryAccessMode.Write;
				if (newMode != _accessMode)
					AccessMode = newMode;
			}
		}

		/// <summary>
		/// Whether execute access is granted.
		/// </summary>
		[JsonIgnore]
		public bool IsExecute
		{
			get => (_accessMode & DirectoryAccessMode.Execute) == DirectoryAccessMode.Execute;
			set
			{
				var newMode = value ? _accessMode | DirectoryAccessMode.Execute : _accessMode & ~DirectoryAccessMode.Execute;
				if (newMode != _accessMode)
					AccessMode = newMode;
			}
		}

		/// <summary>
		/// Whether access is denied (no flags set).
		/// </summary>
		[JsonIgnore]
		public bool IsAccessDenied => _accessMode == DirectoryAccessMode.None;
	}
}
