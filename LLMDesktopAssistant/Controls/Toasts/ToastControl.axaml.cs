using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Material.Icons.Avalonia;

namespace LLMDesktopAssistant.Controls.Toasts;

/// <summary>
/// A container control that displays toast notifications with composition animations.
/// Toasts slide in from the edge and can auto-dismiss after a configurable duration.
/// </summary>
public partial class ToastControl : UserControl
{
	// ── Constants ────────────────────────────────────────────────

	private const double SlideInDistance = 80.0;
	private static readonly TimeSpan SlideInDuration = TimeSpan.FromMilliseconds(350);
	private static readonly TimeSpan FadeInDuration = TimeSpan.FromMilliseconds(250);
	private static readonly TimeSpan SlideOutDuration = TimeSpan.FromMilliseconds(300);

	// ── Styled Properties ────────────────────────────────────────

	/// <summary>
	/// Defines where on the screen toasts are positioned.
	/// </summary>
	public static readonly StyledProperty<ToastsPlacement> PlacementProperty =
		AvaloniaProperty.Register<ToastControl, ToastsPlacement>(
			nameof(Placement), ToastsPlacement.TopRight);

	/// <summary>
	/// Defines the direction in which toasts stack (top-to-bottom or bottom-to-top).
	/// </summary>
	public static readonly StyledProperty<ToastsDirection> DirectionProperty =
		AvaloniaProperty.Register<ToastControl, ToastsDirection>(
			nameof(Direction), ToastsDirection.FromTopToBottom);

	/// <summary>
	/// Maximum number of toasts visible at once. Older toasts are hidden when exceeded.
	/// </summary>
	public static readonly StyledProperty<int> MaxVisibleToastsProperty =
		AvaloniaProperty.Register<ToastControl, int>(
			nameof(MaxVisibleToasts), 5);

	public ToastsPlacement Placement
	{
		get => GetValue(PlacementProperty);
		set => SetValue(PlacementProperty, value);
	}

	public ToastsDirection Direction
	{
		get => GetValue(DirectionProperty);
		set => SetValue(DirectionProperty, value);
	}

	public int MaxVisibleToasts
	{
		get => GetValue(MaxVisibleToastsProperty);
		set => SetValue(MaxVisibleToastsProperty, value);
	}

	// ── Observable Collection ────────────────────────────────────

	/// <summary>
	/// The collection of currently displayed toast ViewModels.
	/// </summary>
	public ObservableCollection<ToastItemViewModel> Toasts { get; } = [];

	// ── Private Fields ───────────────────────────────────────────

	private readonly Dictionary<long, CompositionVisual?> _toastVisuals = [];
	private readonly Dictionary<long, CancellationTokenSource> _toastTimers = [];

	// ── Constructor ──────────────────────────────────────────────

	static ToastControl()
	{
		// Update placement when property changes
		PlacementProperty.Changed.AddClassHandler<ToastControl>((ctrl, _) => ctrl.UpdatePlacement());
		DirectionProperty.Changed.AddClassHandler<ToastControl>((ctrl, _) => ctrl.UpdateDirection());
	}

	public ToastControl()
	{
		InitializeComponent();

		PART_ToastList.ItemsSource = Toasts;
		PART_ToastList.ContainerPrepared += OnItemContainerPrepared;

		// Apply initial placement and direction
		UpdatePlacement();
		UpdateDirection();
	}

	// ── Public Methods ───────────────────────────────────────────

	/// <summary>
	/// Shows an info toast.
	/// </summary>
	public void ShowInfo(string title, string? description = null, double durationSeconds = 5.0)
	{
		Show(new ToastItemViewModel
		{
			Type = ToastType.Info,
			Title = title,
			Description = description,
			DurationSeconds = durationSeconds,
			DismissCommand = CreateDismissCommand(),
		});
	}

	/// <summary>
	/// Shows a warning toast.
	/// </summary>
	public void ShowWarning(string title, string? description = null, double durationSeconds = 6.0)
	{
		Show(new ToastItemViewModel
		{
			Type = ToastType.Warning,
			Title = title,
			Description = description,
			DurationSeconds = durationSeconds,
			DismissCommand = CreateDismissCommand(),
		});
	}

	/// <summary>
	/// Shows an error toast.
	/// </summary>
	public void ShowError(string title, string? description = null, double durationSeconds = 8.0)
	{
		Show(new ToastItemViewModel
		{
			Type = ToastType.Error,
			Title = title,
			Description = description,
			DurationSeconds = durationSeconds,
			DismissCommand = CreateDismissCommand(),
		});
	}

	/// <summary>
	/// Shows a success toast.
	/// </summary>
	public void ShowSuccess(string title, string? description = null, double durationSeconds = 5.0)
	{
		Show(new ToastItemViewModel
		{
			Type = ToastType.Success,
			Title = title,
			Description = description,
			DurationSeconds = durationSeconds,
			DismissCommand = CreateDismissCommand(),
		});
	}

	/// <summary>
	/// Shows a custom toast with the provided ViewModel.
	/// </summary>
	public void Show(ToastItemViewModel toast)
	{
		ArgumentNullException.ThrowIfNull(toast);

		// Add the toast
		Toasts.Add(toast);

		// Enforce max visible limit
		EnforceMaxVisible();

		// Schedule auto-dismiss if duration is positive
		if (toast.DurationSeconds > 0)
		{
			ScheduleDismiss(toast.Id, TimeSpan.FromSeconds(toast.DurationSeconds));
		}
	}

	/// <summary>
	/// Dismisses a specific toast by its ID with an exit animation.
	/// </summary>
	public void Dismiss(long toastId)
	{
		if (!Dispatcher.UIThread.CheckAccess())
		{
			Dispatcher.UIThread.Post(() => Dismiss(toastId));
			return;
		}

		var toast = Toasts.FirstOrDefault(t => t.Id == toastId);
		if (toast == null) return;

		// Cancel any pending auto-dismiss timer
		CancelTimer(toastId);

		// Mark as not visible
		toast.IsVisible = false;

		// Animate out and remove
		AnimateOut(toastId, () =>
		{
			Toasts.Remove(toast);
			_toastVisuals.Remove(toastId);
		});
	}

	/// <summary>
	/// Dismisses all currently visible toasts.
	/// </summary>
	public void DismissAll()
	{
		var ids = Toasts.Select(t => t.Id).ToList();
		foreach (var id in ids)
		{
			Dismiss(id);
		}
	}

	// ── Container Lifecycle ──────────────────────────────────────

	private void OnItemContainerPrepared(object? sender, ContainerPreparedEventArgs e)
	{
		// Find the toast ViewModel from the container's DataContext
		if (e.Container.DataContext is not ToastItemViewModel toast)
			return;

		// Attach composition animations after the element is loaded
		e.Container.Loaded += (_, _) =>
		{
			// Configure icon and color based on toast type
			ConfigureToastVisuals(e.Container, toast);
			AttachAnimationToToast(toast.Id, e.Container);
		};
	}

	private void ConfigureToastVisuals(Control container, ToastItemViewModel toast)
	{
		// Find the icon border and icon
		var cc = container.FindDescendantOfType<Border>()!;
		// Авалония говно, нахуя мне NameScope нужны, если они не работают???
		var iconBorder = cc.FindDescendantOfType<Border>(false, d => d.Name == "PART_IconBorder");
		var icon = cc.FindDescendantOfType<MaterialIcon>(false, d => d.Name == "PART_ToastIcon");

		if (icon == null) return;

		// Set icon and colors based on type
		switch (toast.Type)
		{
			case ToastType.Info:
				icon.Kind = MaterialIconKind.InformationOutline;
				if (iconBorder != null)
					iconBorder.Background = this.TryFindResource("PrimaryAccentBrush", out var accentBrush)
						? accentBrush as IBrush
						: new SolidColorBrush(Color.Parse("#6C5CE7"));
				break;

			case ToastType.Warning:
				icon.Kind = MaterialIconKind.AlertOutline;
				if (iconBorder != null)
					iconBorder.Background = this.TryFindResource("WarningBrush", out var warningBrush)
						? warningBrush as IBrush
						: new SolidColorBrush(Color.Parse("#D4A017"));
				break;

			case ToastType.Error:
				icon.Kind = MaterialIconKind.AlertCircleOutline;
				if (iconBorder != null)
					iconBorder.Background = this.TryFindResource("ErrorBrush", out var errorBrush)
						? errorBrush as IBrush
						: new SolidColorBrush(Color.Parse("#FF4444"));
				break;

			case ToastType.Success:
				icon.Kind = MaterialIconKind.CheckCircleOutline;
				if (iconBorder != null)
					iconBorder.Background = this.TryFindResource("SuccessBrush", out var successBrush)
						? successBrush as IBrush
						: new SolidColorBrush(Color.Parse("#2ECC71"));
				break;
		}
	}

	// ── Private Methods ──────────────────────────────────────────

	private ICommand CreateDismissCommand()
	{
		return new RelayCommand<long>(Dismiss);
	}

	private void ScheduleDismiss(long toastId, TimeSpan delay)
	{
		var cts = new CancellationTokenSource();
		_toastTimers[toastId] = cts;

		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(delay, cts.Token);
				if (!cts.Token.IsCancellationRequested)
				{
					Dispatcher.UIThread.Post(() => Dismiss(toastId));
				}
			}
			catch (OperationCanceledException)
			{
				// Cancelled — do nothing
			}
		}, cts.Token);
	}

	private void CancelTimer(long toastId)
	{
		if (_toastTimers.TryGetValue(toastId, out var cts))
		{
			cts.Cancel();
			cts.Dispose();
			_toastTimers.Remove(toastId);
		}
	}

	private void EnforceMaxVisible()
	{
		while (Toasts.Count > MaxVisibleToasts)
		{
			var oldest = Toasts.FirstOrDefault();
			if (oldest != null)
			{
				Dismiss(oldest.Id);
			}
		}
	}

	// ── Animation System ────────────────────────────────────────

	/// <summary>
	/// Attaches composition animations to a toast's visual after it's been added to the visual tree.
	/// </summary>
	private void AttachAnimationToToast(long toastId, Control container)
	{
		var visual = ElementComposition.GetElementVisual(container);
		if (visual == null) return;

		_toastVisuals[toastId] = visual;

		var compositor = visual.Compositor;

		// ── 1. Implicit animation for Opacity ──
		var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
		opacityAnimation.Duration = FadeInDuration;
		opacityAnimation.Target = "Opacity";
		opacityAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue");

		// ── 2. Implicit animation for Offset (smooth slide) ──
		var offsetAnimation = compositor.CreateVector3DKeyFrameAnimation();
		offsetAnimation.Duration = SlideInDuration;
		offsetAnimation.Target = "Offset";
		offsetAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue");

		var implicitAnimations = compositor.CreateImplicitAnimationCollection();
		implicitAnimations["Opacity"] = opacityAnimation;
		implicitAnimations["Offset"] = offsetAnimation;

		visual.ImplicitAnimations = implicitAnimations;

		// ── 3. Explicit slide-in animation ──
		var (startOffset, endOffset) = GetSlideOffsets();

		// Set the initial offset (slide in from the appropriate direction)
		visual.Offset = startOffset;

		// Start the slide-in animation
		var slideInAnimation = compositor.CreateVector3DKeyFrameAnimation();
		slideInAnimation.Duration = SlideInDuration;
		slideInAnimation.InsertKeyFrame(0f, startOffset);
		slideInAnimation.InsertKeyFrame(1f, endOffset);
		visual.StartAnimation("Offset", slideInAnimation);

		// Also fade in
		visual.Opacity = 0f;
		var fadeInAnim = compositor.CreateScalarKeyFrameAnimation();
		fadeInAnim.Duration = FadeInDuration;
		fadeInAnim.InsertKeyFrame(0f, 0f);
		fadeInAnim.InsertKeyFrame(1f, 1f);
		visual.StartAnimation("Opacity", fadeInAnim);
	}

	/// <summary>
	/// Plays an exit animation (fade + slide out) and invokes the callback when done.
	/// </summary>
	private void AnimateOut(long toastId, Action onComplete)
	{
		if (!_toastVisuals.TryGetValue(toastId, out var visual) || visual == null)
		{
			onComplete();
			return;
		}

		var compositor = visual.Compositor;

		// Calculate exit offset: slide out further in the direction of entry
		var (startOffset, _) = GetSlideOffsets();

		var exitOffset = new Vector3D(
			startOffset.X * 1.5f,
			startOffset.Y * 1.5f,
			0);

		var slideOut = compositor.CreateVector3DKeyFrameAnimation();
		slideOut.Duration = SlideOutDuration;
		slideOut.InsertKeyFrame(1f, exitOffset);
		visual.StartAnimation("Offset", slideOut);

		// Fade out
		var fadeOut = compositor.CreateScalarKeyFrameAnimation();
		fadeOut.Duration = SlideOutDuration;
		fadeOut.InsertKeyFrame(1f, 0f);
		visual.StartAnimation("Opacity", fadeOut);

		// Wait for animation to finish, then invoke callback
		_ = Task.Run(async () =>
		{
			await Task.Delay(SlideOutDuration + TimeSpan.FromMilliseconds(50));
			Dispatcher.UIThread.Post(onComplete);
		});
	}

	/// <summary>
	/// Calculates the start (off-screen) and end (on-screen) offsets based on placement.
	/// </summary>
	private (Vector3D Start, Vector3D End) GetSlideOffsets()
	{
		double startX = 0, startY = 0;

		switch (Placement)
		{
			case ToastsPlacement.TopLeft:
			case ToastsPlacement.CenterLeft:
			case ToastsPlacement.BottomLeft:
				startX = -SlideInDistance;
				break;
			case ToastsPlacement.TopRight:
			case ToastsPlacement.CenterRight:
			case ToastsPlacement.BottomRight:
				startX = SlideInDistance;
				break;
		}

		switch (Direction)
		{
			case ToastsDirection.FromTopToBottom:
				startY = SlideInDistance;
				break;
			case ToastsDirection.FromBottomToTop:
				startY = -SlideInDistance;
				break;
		}

		return (new Vector3D(startX, startY, 0), new Vector3D(0, 0, 0));
	}

	// ── Layout Helpers ───────────────────────────────────────────

	private void UpdatePlacement()
	{
		if (PART_ToastList == null) return;

		switch (Placement)
		{
			case ToastsPlacement.TopLeft:
			case ToastsPlacement.CenterLeft:
			case ToastsPlacement.BottomLeft:
				PART_ToastList.HorizontalAlignment = HorizontalAlignment.Left;
				break;
			case ToastsPlacement.Top:
			case ToastsPlacement.Bottom:
				PART_ToastList.HorizontalAlignment = HorizontalAlignment.Center;
				break;
			case ToastsPlacement.TopRight:
			case ToastsPlacement.CenterRight:
			case ToastsPlacement.BottomRight:
			default:
				PART_ToastList.HorizontalAlignment = HorizontalAlignment.Right;
				break;
		}

		switch (Placement)
		{
			case ToastsPlacement.TopLeft:
			case ToastsPlacement.Top:
			case ToastsPlacement.TopRight:
				PART_ToastList.VerticalAlignment = VerticalAlignment.Top;
				break;
			case ToastsPlacement.CenterLeft:
			case ToastsPlacement.CenterRight:
				PART_ToastList.VerticalAlignment = VerticalAlignment.Center;
				break;
			case ToastsPlacement.BottomLeft:
			case ToastsPlacement.Bottom:
			case ToastsPlacement.BottomRight:
				PART_ToastList.VerticalAlignment = VerticalAlignment.Bottom;
				break;
		}
	}

	private void UpdateDirection()
	{
		if (PART_StackPanel == null) return;

		PART_StackPanel.ReverseOrder = Direction switch
		{
			ToastsDirection.FromTopToBottom => true,
			_ => false,
		};
	}
}
