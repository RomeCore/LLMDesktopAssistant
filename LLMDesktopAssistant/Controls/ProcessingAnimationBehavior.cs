using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;

namespace LLMDesktopAssistant.Core.Controls
{
	public static class ProcessingAnimationBehavior
	{
		private static readonly Dictionary<Border, Tuple<SolidColorBrush, ColorAnimation>>
			_animations = new Dictionary<Border, Tuple<SolidColorBrush, ColorAnimation>>();

		public static readonly DependencyProperty IsProcessingProperty =
			DependencyProperty.RegisterAttached(
				"IsProcessing",
				typeof(bool),
				typeof(ProcessingAnimationBehavior),
				new PropertyMetadata(false, OnIsProcessingChanged));

		public static readonly DependencyProperty NormalColorProperty =
			DependencyProperty.RegisterAttached(
				"NormalColor",
				typeof(SolidColorBrush),
				typeof(ProcessingAnimationBehavior),
				new PropertyMetadata(Brushes.Transparent));

		public static readonly DependencyProperty ProcessingColorProperty =
			DependencyProperty.RegisterAttached(
				"ProcessingColor",
				typeof(SolidColorBrush),
				typeof(ProcessingAnimationBehavior),
				new PropertyMetadata(Brushes.LightBlue));

		public static readonly DependencyProperty AnimationSpeedProperty =
			DependencyProperty.RegisterAttached(
				"AnimationSpeed",
				typeof(double),
				typeof(ProcessingAnimationBehavior),
				new PropertyMetadata(0.8));

		public static void SetIsProcessing(DependencyObject element, bool value)
			=> element.SetValue(IsProcessingProperty, value);

		public static bool GetIsProcessing(DependencyObject element)
			=> (bool)element.GetValue(IsProcessingProperty);

		public static void SetNormalColor(DependencyObject element, SolidColorBrush value)
			=> element.SetValue(NormalColorProperty, value);

		public static SolidColorBrush GetNormalColor(DependencyObject element)
			=> (SolidColorBrush)element.GetValue(NormalColorProperty);

		public static void SetProcessingColor(DependencyObject element, SolidColorBrush value)
			=> element.SetValue(ProcessingColorProperty, value);

		public static SolidColorBrush GetProcessingColor(DependencyObject element)
			=> (SolidColorBrush)element.GetValue(ProcessingColorProperty);

		public static void SetAnimationSpeed(DependencyObject element, double value)
			=> element.SetValue(AnimationSpeedProperty, value);

		public static double GetAnimationSpeed(DependencyObject element)
			=> (double)element.GetValue(AnimationSpeedProperty);

		private static void OnIsProcessingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Border border)
			{
				bool isProcessing = (bool)e.NewValue;

				if (border.Dispatcher.CheckAccess())
				{
					var parent = Window.GetWindow(border);

					if (isProcessing)
						StartAnimation(border);
					else
						StopAnimation(border);
				}
				else
				{
					border.Dispatcher.BeginInvoke(new Action(() =>
					{
						if (isProcessing)
							StartAnimation(border);
						else
							StopAnimation(border);
					}));
				}
			}
		}

		private static void StartAnimation(Border border)
		{
			StopAnimation(border);

			var normalColor = GetNormalColor(border).Color;
			var processingColor = GetProcessingColor(border).Color;
			var speed = GetAnimationSpeed(border);

			var brush = new SolidColorBrush(normalColor);

			border.Background = brush;

			var animation = new ColorAnimation
			{
				From = normalColor,
				To = processingColor,
				Duration = TimeSpan.FromSeconds(speed),
				AutoReverse = true,
				RepeatBehavior = RepeatBehavior.Forever
			};

			_animations[border] = new Tuple<SolidColorBrush, ColorAnimation>(brush, animation);

			brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
		}

		private static void StopAnimation(Border border)
		{
			if (_animations.TryGetValue(border, out var animationInfo))
			{
				var brush = animationInfo.Item1;

				var normalColor = GetNormalColor(border).Color;
				var processingColor = GetProcessingColor(border).Color;
				var speed = GetAnimationSpeed(border);

				var animation = new ColorAnimation
				{
					From = processingColor,
					To = normalColor,
					Duration = TimeSpan.FromSeconds(speed)
				};
				brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);

				_animations.Remove(border);
			}
		}
	}
}