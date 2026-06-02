using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using Serilog;

namespace LLMDesktopAssistant.Controls;

[TemplatePart("PART_RootGrid", typeof(Grid))]
[TemplatePart("PART_Track", typeof(Avalonia.Controls.Shapes.Path))]
[TemplatePart("PART_Indicator", typeof(Avalonia.Controls.Shapes.Path))]
public class ProgressRing : TemplatedControl
{
	public static readonly StyledProperty<double> MinimumProperty =
		AvaloniaProperty.Register<ProgressRing, double>(
			nameof(Minimum), 0.0);

	public static readonly StyledProperty<double> MaximumProperty =
		AvaloniaProperty.Register<ProgressRing, double>(
			nameof(Maximum), 100.0);

	public static readonly StyledProperty<double> ValueProperty =
		AvaloniaProperty.Register<ProgressRing, double>(
			nameof(Value), 0.0);

	public static readonly StyledProperty<bool> IsIndeterminateProperty =
		AvaloniaProperty.Register<ProgressRing, bool>(
			nameof(IsIndeterminate), false);

	public static readonly StyledProperty<double> StrokeThicknessProperty =
		AvaloniaProperty.Register<ProgressRing, double>(
			nameof(StrokeThickness), 2.0);

	public double Minimum
	{
		get => GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	public double Maximum
	{
		get => GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	public double Value
	{
		get => GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public bool IsIndeterminate
	{
		get => GetValue(IsIndeterminateProperty);
		set => SetValue(IsIndeterminateProperty, value);
	}

	public double StrokeThickness
	{
		get => GetValue(StrokeThicknessProperty);
		set => SetValue(StrokeThicknessProperty, value);
	}

	private Avalonia.Controls.Shapes.Path? _trackPath;
	private Avalonia.Controls.Shapes.Path? _indicatorPath;
	private Grid? _rootGrid;

	private DispatcherTimer? _spinnerTimer;
	private double _spinnerTime;

	static ProgressRing()
	{
		MinimumProperty.Changed.AddClassHandler<ProgressRing>((o, _) => o.InvalidateProgress());
		MaximumProperty.Changed.AddClassHandler<ProgressRing>((o, _) => o.InvalidateProgress());
		ValueProperty.Changed.AddClassHandler<ProgressRing>((o, _) => o.InvalidateProgress());
		IsIndeterminateProperty.Changed.AddClassHandler<ProgressRing>((o, e) =>
		{
			o.OnIsIntermediateChanged((bool)e.NewValue!);
		});
		StrokeThicknessProperty.Changed.AddClassHandler<ProgressRing>((o, _) => o.InvalidateProgress());

		AffectsRender<ProgressRing>(
			MinimumProperty,
			MaximumProperty,
			ValueProperty,
			IsIndeterminateProperty,
			WidthProperty,
			HeightProperty,
			ForegroundProperty);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_rootGrid = e.NameScope.Find<Grid>("PART_RootGrid");
		_trackPath = e.NameScope.Find<Avalonia.Controls.Shapes.Path>("PART_Track");
		_indicatorPath = e.NameScope.Find<Avalonia.Controls.Shapes.Path>("PART_Indicator");

		InvalidateProgress();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == BoundsProperty)
		{
			InvalidateProgress();
		}
	}

	private void InvalidateProgress()
	{
		if (_trackPath == null || _indicatorPath == null)
			return;

		var size = Math.Min(Bounds.Width, Bounds.Height);
		if (size <= 0)
			size = Math.Min(Width, Height);
		if (size <= 0)
			size = 40; // fallback default

		var center = new Point(size / 2, size / 2);
		var radius = (size - StrokeThickness) / 2;

		if (IsIndeterminate)
		{
			RenderIndeterminate(center, radius);
		}
		else
		{
			RenderDeterminate(center, radius);
		}
	}

	private void RenderDeterminate(Point center, double radius)
	{
		if (_trackPath == null || _indicatorPath == null)
			return;

		// Normalise the value
		var min = Minimum;
		var max = Maximum;

		// Guard against degenerate ranges
		if (double.IsNaN(min) || double.IsNaN(max) || double.IsInfinity(min) || double.IsInfinity(max))
			return;
		if (Math.Abs(max - min) < double.Epsilon)
			return;

		var value = Math.Clamp(Value, min, max);
		var progress = (value - min) / (max - min);
		progress = Math.Clamp(progress, 0.0, 1.0);

		// Full circle is 360, we start from top (270 or -90)
		const double startAngle = -90.0; // 12 o'clock
		var sweepAngle = progress * 360.0;

		// Build track (always full circle)
		_trackPath.Data = CreateArcGeometry(center, radius, 0, 360);

		// Build indicator arc
		if (sweepAngle > 0)
		{
			_indicatorPath.Data = CreateArcGeometry(center, radius, startAngle, sweepAngle);
			_indicatorPath.IsVisible = true;
		}
		else
		{
			_indicatorPath.Data = null;
			_indicatorPath.IsVisible = false;
		}

		// Update stroke fill from Foreground
		_indicatorPath.Stroke = Foreground;
		_trackPath.Stroke = Foreground;
	}

	private static double UpDownModulus(double value, double modulus)
	{
		value %= modulus * 2; // Wrap around twice the modulus
		if (value >= modulus)
			return modulus * 2 - value;
		return value;
	}

	private void RenderIndeterminate(Point center, double radius)
	{
		if (_trackPath == null || _indicatorPath == null)
			return;

		// Show a partial arc that rotates
		_trackPath.Data = CreateArcGeometry(center, radius, 0, 360);

		var startAngle = (_spinnerTime * 360 * 3) % 360;
		var sweepAngle = UpDownModulus(_spinnerTime * 180, 180);
		_indicatorPath.Data = CreateArcGeometry(center, radius, startAngle, sweepAngle);
		_indicatorPath.Stroke = Foreground;
		_indicatorPath.IsVisible = true;

		_trackPath.Stroke = Foreground;
	}

	/// <summary>
	/// Creates a <see cref="StreamGeometry"/> that describes a circular arc.
	/// </summary>
	private static StreamGeometry CreateArcGeometry(Point center, double radius, double startAngleDeg, double sweepDeg)
	{
		var geometry = new StreamGeometry();

		using (var ctx = geometry.Open())
		{
			// Convert degrees to radians
			var startRad = startAngleDeg * Math.PI / 180.0;
			var sweepRad = sweepDeg * Math.PI / 180.0;
			var endRad = startRad + sweepRad;

			// Start point
			var startPoint = new Point(
				center.X + radius * Math.Cos(startRad),
				center.Y + radius * Math.Sin(startRad));

			// End point
			var endPoint = new Point(
				center.X + radius * Math.Cos(endRad),
				center.Y + radius * Math.Sin(endRad));

			// Is the arc larger than 180°?
			var isLargeArc = sweepDeg > 180.0;

			// Sweep direction: clockwise
			var sweepDirection = SweepDirection.Clockwise;

			ctx.BeginFigure(startPoint, false);
			ctx.ArcTo(endPoint, new Size(radius, radius), 0, isLargeArc, sweepDirection);
			ctx.EndFigure(false);
		}

		return geometry;
	}

	private void OnIsIntermediateChanged(bool isIntermediate)
	{
		if (isIntermediate)
		{
			StartSpinner();
		}
		else
		{
			StopSpinner();
		}

		InvalidateProgress();
	}

	private void StartSpinner()
	{
		StopSpinner();

		_spinnerTime = 0;
		_spinnerTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(10) // ~100 fps
		};
		_spinnerTimer.Tick += OnSpinnerTick;
		_spinnerTimer.Start();
	}

	private void StopSpinner()
	{
		if (_spinnerTimer != null)
		{
			_spinnerTimer.Stop();
			_spinnerTimer.Tick -= OnSpinnerTick;
			_spinnerTimer = null;
		}
	}

	private void OnSpinnerTick(object? sender, EventArgs e)
	{
		_spinnerTime += _spinnerTimer!.Interval.TotalSeconds;
		InvalidateProgress();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);
		StopSpinner();
	}
}
