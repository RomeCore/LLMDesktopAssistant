using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ControlzEx.Theming;
using MaterialDesignThemes.Wpf;

namespace LLMDesktopAssistant.Core.Services.Instances
{
	/// <summary>
	/// Represents the type of theme to be used.
	/// </summary>
	public enum ThemeType
	{
		Light,
		Dark
	}

	[Service]
	public class ThemeService
	{
		private readonly PaletteHelper _mdPaletteHelper; // MaterialDesignThemes palette helper
		private readonly ThemeManager _maThemeManager;   // MahApps.Metro theme manager

		public ThemeService()
		{
			_mdPaletteHelper = new PaletteHelper();
			_maThemeManager = ThemeManager.Current;
		}

		private ThemeType _themeType = ThemeType.Light;
		/// <summary>
		/// Gets or sets the type of theme.
		/// </summary>
		public ThemeType ThemeType
		{
			get => _themeType;
			set
			{
				if (_themeType == value) return;
				_themeType = value;

				UpdateMaterialDesignTheme();
				UpdateMahAppsTheme();
			}
		}

		private Color _primaryColor = Colors.Black;
		/// <summary>
		/// Gets or sets the primary color of the theme.
		/// </summary>
		public Color PrimaryColor
		{
			get => _primaryColor;
			set
			{
				if (_primaryColor == value) return;
				_primaryColor = value;

				UpdateMaterialDesignTheme();
				UpdateMahAppsTheme();
			}
		}

		private Color _accentColor = Colors.Black;
		/// <summary>
		/// Gets or sets the secondary color of the theme.
		/// </summary>
		public Color SecondaryColor
		{
			get => _accentColor;
			set
			{
				if (_accentColor == value) return;

				UpdateMaterialDesignTheme();
			}
		}

		private void UpdateMaterialDesignTheme()
		{
			var theme = _mdPaletteHelper.GetTheme();

			switch (_themeType)
			{
				case ThemeType.Light:
					theme.SetLightTheme();
					break;
				case ThemeType.Dark:
					theme.SetDarkTheme();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(ThemeType), "Invalid ThemeType");
			}

			theme.SetPrimaryColor(_primaryColor);
			theme.SetSecondaryColor(_accentColor);

			_mdPaletteHelper.SetTheme(theme);
		}

		private void UpdateMahAppsTheme()
		{
			var themeString = _themeType switch
			{
				ThemeType.Light => "Light",
				ThemeType.Dark => "Dark",
				_ => throw new ArgumentOutOfRangeException(nameof(ThemeType), "Invalid ThemeType")
			};

			var theme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme(
				themeString, _primaryColor);
			if (theme != null)
				_maThemeManager.ChangeTheme(App.Current, theme);
		}
	}
}