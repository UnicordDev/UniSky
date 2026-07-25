using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Animations.Expressions;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using EF = Microsoft.Toolkit.Uwp.UI.Animations.Expressions.ExpressionFunctions;

namespace UniSky.Controls.Sheet;

/// <summary>
/// Hosts a single sheet and handles its gestures.
///
/// On small screens the sheet is positioned by the vertical scroll offset of <c>Scroller</c> (see
/// <see cref="SheetContentPanel"/>), so a drag on the sheet, or an over-pull of the sheet's own
/// scrollable content, moves the sheet one-to-one with the finger, and the system provides the inertia,
/// rubber banding and snapping to a detent. 
///
/// On large screens the sheet is a centred card shown and hidden by storyboard instead.
/// </summary>
public sealed partial class SheetPresenter : UserControl
{
    private const double MediumWindowWidth = 621;
    private const double SheetTopGap = 16;
    private const double OverscrollSlop = 0;
    private const double CardMaxHeight = 400;
    private const float PushBackScale = 0.92f;

    // adjusts how far up the backdrop gets pushed so it remains visible
    private const double BackdropPeek = 24;

    private static readonly Vector2 EaseOutExpo1 = new Vector2(0.16f, 1.0f);
    private static readonly Vector2 EaseOutExpo2 = new Vector2(0.30f, 1.0f);
    private static readonly TimeSpan EntryDuration = TimeSpan.FromSeconds(0.5);

    private const string PROGRESS_NODE = "progress";
    private const string ENTRY_OFFSET_NODE = "entryOffset";
    private const string DETENT_OFFSET_NODE = "detentOffset";
    private const string PUSH_BACK_SCALE_NODE = "pushBackScale";
    private const string PUSH_BACK_OFFSET_NODE = "pushBackOffset";

    private enum SheetState
    {
        Hidden,
        Presenting,
        Presented,
        Dismissing
    }

    private Compositor _compositor;
    private CompositionPropertySet _props;
    private CompositionPropertySet _scrollerPropertySet;
    private Visual _dimVisual;
    private Visual _sheetVisual;
    private Visual _backdropVisual;
    private FrameworkElement _backdropTarget;

    private SheetState _state = SheetState.Hidden;
    private TaskCompletionSource<object> _presentTcs;
    private TaskCompletionSource<object> _dismissTcs;
    private bool _isLoaded;
    private bool _presentPending;
    private bool _awaitingPresentOffset;
    private bool _isCardMode;
    private bool _dimAnimationStarted;
    private bool _shadowApplied;
    private bool _shadowRefreshQueued;
    private double _topInset;
    private Thickness _safeArea;

    public double ContentTop
        => _isCardMode ? 0 : _topInset;

    public event TypedEventHandler<SheetPresenter, object> DismissRequested;

    public FrameworkElement SheetContent
        => (FrameworkElement)SheetRoot.Content;

    public FrameworkElement BackdropTarget
        => _backdropTarget;
        
    public bool IsAtDismissedPosition
        => _isCardMode ? _state == SheetState.Hidden : Scroller.VerticalOffset <= 1;

    public SheetPresenter()
    {
        this.InitializeComponent();

        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
        this.SizeChanged += OnSizeChanged;

        VisualStateManager.GoToState(this, "CardHidden", false);
    }

    public void SetContent(FrameworkElement content)
        => SheetRoot.Content = content;

    public void SetBackdropTarget(FrameworkElement target)
    {
        if (_backdropTarget == target)
            return;

        DetachBackdrop();
        _backdropTarget = target;
        RefreshBackdrop();
        UpdatePushBackOffset();
    }

    public void UpdateSafeArea(Thickness safeArea)
    {
        _safeArea = safeArea;
        UpdateSizing();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        UpdateSizing();

        if (_presentPending)
        {
            _presentPending = false;
            BeginPresent();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
        => _isLoaded = false;

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSizing();

        if (_state == SheetState.Presented && !_isCardMode)
        {
            // wait a tick for the scrollview to realise layout has changeed
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => Scroller.ChangeView(null, ContentPanel.DetentOffset, null, true));
        }
    }

    private void UpdateSizing()
    {
        var bounds = Window.Current.Bounds;
        var width = ActualWidth > 0 ? ActualWidth : bounds.Width;
        var height = ActualHeight > 0 ? ActualHeight : bounds.Height;

        if (width <= 0 || height <= 0)
            return;

        _isCardMode = bounds.Width >= MediumWindowWidth;

        var topInset = SheetTopGap + _safeArea.Top;
        _topInset = topInset;

        ContentPanel.IsCardMode = _isCardMode;
        ContentPanel.ViewportWidth = width;
        ContentPanel.ViewportHeight = height;
        ContentPanel.OverscrollSlop = OverscrollSlop;

        if (_isCardMode)
        {
            var avoidance = GetAvoidancePadding();

            ContentPanel.TopInset = 0;
            Scroller.VerticalScrollMode = ScrollMode.Disabled;
            SheetBorder.Margin = new Thickness(avoidance.Left, topInset, avoidance.Right, 0);
            SheetRoot.Height = Math.Max(0, Math.Min(CardMaxHeight, height - topInset));
        }
        else
        {
            ContentPanel.TopInset = topInset;
            Scroller.VerticalScrollMode = ScrollMode.Enabled;
            SheetBorder.Margin = new Thickness(_safeArea.Left, 0, _safeArea.Right, 0);
            SheetRoot.Height = Math.Max(0, height - topInset - _safeArea.Bottom);
        }


        if (!_isCardMode)
            VisualStateManager.GoToState(this, "CardNormal", false);
        else if (_state == SheetState.Hidden)
            VisualStateManager.GoToState(this, "CardHidden", false);

        RequestShadowRefresh();
        EnsureComposition();

        // guard the divide in the progress expression
        _props.InsertScalar(DETENT_OFFSET_NODE, (float)Math.Max(1, ContentPanel.DetentOffset));

        RefreshDim();
        RefreshBackdrop();
        UpdatePushBackOffset();
    }

    private void EnsureComposition()
    {
        if (_props != null)
            return;

        _scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(Scroller);
        _compositor = _scrollerPropertySet.Compositor;

        _props = _compositor.CreatePropertySet();
        _props.InsertScalar(PROGRESS_NODE, 0);
        _props.InsertScalar(ENTRY_OFFSET_NODE, 0);
        _props.InsertScalar(DETENT_OFFSET_NODE, 1);
        _props.InsertScalar(PUSH_BACK_SCALE_NODE, PushBackScale);
        _props.InsertScalar(PUSH_BACK_OFFSET_NODE, 0);

        var scrollingProperties = _scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();
        var props = _props.GetReference();
        var detentNode = props.GetScalarProperty(DETENT_OFFSET_NODE);
        var entryNode = props.GetScalarProperty(ENTRY_OFFSET_NODE);

        var scrollNode = -scrollingProperties.Translation.Y;

        // the sheet sits wherever the scroll offset puts it, pushed back down by entryOffset. a
        // gesture moves the former and leaves the latter at zero; a programmatic show or hide pins
        // the scroller at the detent and animates the latter, so both paths share one position and
        // one progress value.
        _props.StartAnimation(PROGRESS_NODE, EF.Clamp((scrollNode - entryNode) / detentNode, 0, 1));

        var overshootNode = EF.Max(0, scrollNode - detentNode);
        ElementCompositionPreview.SetIsTranslationEnabled(SheetBorder, true);
        _sheetVisual = ElementCompositionPreview.GetElementVisual(SheetBorder);
        _sheetVisual.StartAnimation("Translation", EF.Vector3(0, entryNode + overshootNode, 0));

        _dimVisual = ElementCompositionPreview.GetElementVisual(DimBorder);
    }

    private static Thickness GetAvoidancePadding()
    {
        if (Application.Current.Resources.TryGetValue("SheetRootNavigationViewAvoidancePadding", out var value)
            && value is Thickness thickness)
        {
            return thickness;
        }

        return new Thickness();
    }

    private void RequestShadowRefresh()
    {
        if (_shadowApplied == _isCardMode || _shadowRefreshQueued)
            return;

        _shadowRefreshQueued = true;
        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            _shadowRefreshQueued = false;

            if (_shadowApplied == _isCardMode)
                return;

            if (_isCardMode)
            {
                CommonShadow.CastTo = DimBorder;
                Effects.SetShadow(SheetBorder, CommonShadow);
            }
            else
            {
                Effects.SetShadow(SheetBorder, null);
            }

            _shadowApplied = _isCardMode;
        });
    }

    private void RefreshDim()
    {
        if (_isCardMode || _dimAnimationStarted || _dimVisual == null)
            return;

        _dimVisual.StartAnimation("Opacity", _props.GetReference().GetScalarProperty(PROGRESS_NODE));
        _dimAnimationStarted = true;
    }

    private void UpdatePushBackOffset()
    {
        if (_props == null)
            return;

        var targetTop = _backdropTarget is SheetPresenter sheet ? sheet.ContentTop : 0;
        var offset = (_topInset - BackdropPeek) - (targetTop * PushBackScale);

        _props.InsertScalar(PUSH_BACK_OFFSET_NODE, (float)offset);
    }

    private void RefreshBackdrop()
    {
        if (_backdropTarget == null || _props == null || _isCardMode)
            return;

        if (_backdropVisual == null)
        {
            ElementCompositionPreview.SetIsTranslationEnabled(_backdropTarget, true);
            _backdropVisual = ElementCompositionPreview.GetElementVisual(_backdropTarget);

            var props = _props.GetReference();
            var progressNode = props.GetScalarProperty(PROGRESS_NODE);

            var scaleAnimation = EF.Lerp(1, props.GetScalarProperty(PUSH_BACK_SCALE_NODE), progressNode);
            _backdropVisual.StartAnimation("Scale.X", scaleAnimation);
            _backdropVisual.StartAnimation("Scale.Y", scaleAnimation);
            _backdropVisual.StartAnimation("Translation",
                EF.Vector3(0, EF.Lerp(0, props.GetScalarProperty(PUSH_BACK_OFFSET_NODE), progressNode), 0));
        }

        _backdropVisual.CenterPoint = new Vector3((float)(_backdropTarget.ActualWidth / 2), 0, 0);
    }

    private void DetachBackdrop()
    {
        if (_backdropVisual == null)
            return;

        _backdropVisual.StopAnimation("Scale.X");
        _backdropVisual.StopAnimation("Scale.Y");
        _backdropVisual.StopAnimation("Translation");
        _backdropVisual.Scale = Vector3.One;
        _backdropVisual.Properties.InsertVector3("Translation", Vector3.Zero);
        _backdropVisual = null;
        _backdropTarget = null;
    }

    public Task PresentAsync()
    {
        _presentTcs?.TrySetResult(null);

        var tcs = new TaskCompletionSource<object>();
        _presentTcs = tcs;

        if (!_isLoaded)
            _presentPending = true;
        else
            BeginPresent();

        return tcs.Task;
    }

    private void BeginPresent()
    {
        _state = SheetState.Presenting;

        UpdateSizing();

        if (_isCardMode)
        {
            VisualStateManager.GoToState(this, "CardShown", true);
            return;
        }

        UpdateLayout();

        var detent = ContentPanel.DetentOffset;
        if (detent <= 0)
        {
            CompletePresent();
            return;
        }


        SetEntryOffset((float)detent);

        if (Scroller.VerticalOffset >= detent - 1)
        {
            AnimateEntryOffset(0, CompletePresent);
            return;
        }

        _awaitingPresentOffset = true;
        Scroller.ChangeView(null, detent, null, true);

        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
            if (_state != SheetState.Presenting || !_awaitingPresentOffset)
                return;

            Scroller.ChangeView(null, ContentPanel.DetentOffset, null, true);
            StartPresentAnimation();
        });
    }

    private void StartPresentAnimation()
    {
        _awaitingPresentOffset = false;
        AnimateEntryOffset(0, CompletePresent);
    }

    private void SetEntryOffset(float value)
    {
        _props.StopAnimation(ENTRY_OFFSET_NODE);
        _props.InsertScalar(ENTRY_OFFSET_NODE, value);
    }

    private void AnimateEntryOffset(float to, Action completed)
    {
        var easing = _compositor.CreateCubicBezierEasingFunction(EaseOutExpo1, EaseOutExpo2);

        var animation = _compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(1.0f, to, easing);
        animation.Duration = EntryDuration;

        var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (o, e) => completed();

        _props.StartAnimation(ENTRY_OFFSET_NODE, animation);

        batch.End();
    }

    public Task DismissAsync()
    {
        _dismissTcs?.TrySetResult(null);

        if (_isCardMode)
        {
            var cardTcs = new TaskCompletionSource<object>();
            _dismissTcs = cardTcs;
            _state = SheetState.Dismissing;
            VisualStateManager.GoToState(this, "CardClosed", true);
            return cardTcs.Task;
        }

        if (!_isLoaded || _state == SheetState.Hidden || IsAtDismissedPosition)
        {
            _state = SheetState.Hidden;
            return Task.FromResult<object>(null);
        }

        var tcs = new TaskCompletionSource<object>();
        _dismissTcs = tcs;
        _state = SheetState.Dismissing;

        AnimateEntryOffset((float)ContentPanel.DetentOffset, CompleteDismiss);

        return tcs.Task;
    }

    public void Teardown()
    {
        _state = SheetState.Hidden;
        _presentTcs?.TrySetResult(null);
        _dismissTcs?.TrySetResult(null);
        _presentTcs = null;
        _dismissTcs = null;

        DetachBackdrop();

        if (_props != null)
            _props.StopAnimation(ENTRY_OFFSET_NODE);

        if (_dimAnimationStarted)
        {
            _dimVisual.StopAnimation("Opacity");
            _dimVisual.Opacity = 0;
            _dimAnimationStarted = false;
        }

        SheetRoot.Content = null;
    }

    private void CompletePresent()
    {
        _state = SheetState.Presented;

        var tcs = _presentTcs;
        _presentTcs = null;
        tcs?.TrySetResult(null);
    }

    private void CompleteDismiss()
    {
        _state = SheetState.Hidden;

        var tcs = _dismissTcs;
        _dismissTcs = null;
        tcs?.TrySetResult(null);
    }

    private void Scroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_isCardMode)
            return;

        if (_awaitingPresentOffset && Scroller.VerticalOffset >= ContentPanel.DetentOffset - 1)
        {
            StartPresentAnimation();
            return;
        }

        if (e.IsIntermediate)
            return;

        if (_state != SheetState.Presented || Scroller.VerticalOffset > 1)
            return;

        _state = SheetState.Hidden;
        DismissRequested?.Invoke(this, null);
    }

    private void ShowCardStoryboard_Completed(object sender, object e)
        => CompletePresent();

    private void HideCardStoryboard_Completed(object sender, object e)
        => CompleteDismiss();
}
