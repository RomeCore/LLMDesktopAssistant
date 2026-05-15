using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;

namespace LLMDesktopAssistant.Controls;

/// <summary>
/// Represents a circular progress indicator that supports both determinate
/// (value-based) and indeterminate (spinning animation) modes.
///
/// Properties:
///   Minimum        - The lower bound of the value range (default: 0.0)
///   Maximum        - The upper bound of the value range (default: 100.0)
///   Value          - The current progress value within [Minimum, Maximum]
///   IsIntermediate - When true, the control ignores Value and displays an
///                    indeterminate spinning animation (e.g. loading spinner)
///
/// Template parts:
///   PART_RootGrid    - Grid root element
///   PART_Track       - Path that renders the background track ring
///   PART_Indicator   - Path that renders the progress arc
/// </summary>
public class ProgressRing : TemplatedControl
{
	// ───────────────────────────────────────────────────────────────
	// Dependency properties
	// ───────────────────────────────────────────────────────────────

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

	// ───────────────────────────────────────────────────────────────
	// CLR property wrappers
	// ───────────────────────────────────────────────────────────────

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

	// ───────────────────────────────────────────────────────────────
	// Template parts
	// ───────────────────────────────────────────────────────────────
	
	private Avalonia.Controls.Shapes.Path? _trackPath;
	private Avalonia.Controls.Shapes.Path? _indicatorPath;
	private Grid? _rootGrid;

	// ───────────────────────────────────────────────────────────────
	// Animation state
	// ───────────────────────────────────────────────────────────────

	private DispatcherTimer? _spinnerTimer;
	private double _spinnerAngle;

	// ───────────────────────────────────────────────────────────────
	// Stroke thickness (can be made a property later)
	// ───────────────────────────────────────────────────────────────

	private const double StrokeThickness = 4.0;

	// ───────────────────────────────────────────────────────────────
	// Static constructor — property change handlers
	// ───────────────────────────────────────────────────────────────

	static ProgressRing()
	{
		MinimumProperty.Changed.AddClassHandler<ProgressRing>((o, _) => o.InvalidateProgress());
		MaximumProperty.Changed.AddClassHandler<ProgressRing>((o, _) => o.InvalidateProgress());
		ValueProperty.Changed.AddClassHandler<ProgressRing>((o, _) => o.InvalidateProgress());
		IsIndeterminateProperty.Changed.AddClassHandler<ProgressRing>((o, e) =>
		{
			o.OnIsIntermediateChanged((bool)e.NewValue!);
		});

		AffectsRender<ProgressRing>(
			MinimumProperty,
			MaximumProperty,
			ValueProperty,
			IsIndeterminateProperty,
			WidthProperty,
			HeightProperty,
			ForegroundProperty);
	}

	// ───────────────────────────────────────────────────────────────
	// Lifecycle
	// ───────────────────────────────────────────────────────────────

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

	// ───────────────────────────────────────────────────────────────
	// Progress rendering
	// ───────────────────────────────────────────────────────────────

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

		// Full circle is 360°, we start from top (270° or -90°)
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

	private void RenderIndeterminate(Point center, double radius)
	{
		if (_trackPath == null || _indicatorPath == null)
			return;

		// Show a partial arc that rotates
		const double arcAngle = 120.0; // 120° visible arc
		_trackPath.Data = CreateArcGeometry(center, radius, 0, 360);

		var startAngle = _spinnerAngle;
		var sweepAngle = arcAngle;
		_indicatorPath.Data = CreateArcGeometry(center, radius, startAngle, sweepAngle);
		_indicatorPath.Stroke = Foreground;
		_indicatorPath.IsVisible = true;

		_trackPath.Stroke = Foreground;
	}

	// ───────────────────────────────────────────────────────────────
	// Geometry helpers
	// ───────────────────────────────────────────────────────────────

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

	// ───────────────────────────────────────────────────────────────
	// Intermediate mode handling
	// ───────────────────────────────────────────────────────────────

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

		_spinnerAngle = 0;
		_spinnerTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(16) // ~60 fps
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
		_spinnerAngle = (_spinnerAngle + 6.0) % 360.0; // 6° per frame ≈ 1 rotation/sec
		InvalidateProgress();
	}

	// ───────────────────────────────────────────────────────────────
	// Cleanup
	// ───────────────────────────────────────────────────────────────

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);
		StopSpinner();
	}
}
