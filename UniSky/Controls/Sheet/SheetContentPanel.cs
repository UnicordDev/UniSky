using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace UniSky.Controls.Sheet;

/// <summary>
/// Arranges a sheet inside the presenter's <see cref="ScrollViewer"/> so that the vertical scroll
/// offset <em>is</em> the sheet's position, and reports the detents as snap points.
///
/// <code>
/// contentY 0                    offset 0            sheet entirely below the fold (dismissed)
/// contentY ViewportHeight       top of the sheet
/// contentY ViewportHeight + B   bottom of the sheet
/// </code>
///
/// With the sheet starting a whole viewport down the content, an offset of zero puts its top edge
/// exactly at the bottom of the window, and an offset of <see cref="DetentOffset"/> rests it at
/// <see cref="TopInset"/>. The sheet is <see cref="OverscrollSlop"/> taller than it needs to be so
/// that pulling up past the presented detent reveals more sheet rather than a gap.
///
/// Snap points come from <see cref="GetIrregularSnapPoints"/> rather than from child boundaries, so
/// adding a medium detent later is a change to that list and nothing else.
/// </summary>
internal sealed class SheetContentPanel : Panel, IScrollSnapPointsInfo
{
    private double _viewportWidth;
    private double _viewportHeight;
    private double _topInset;
    private double _overscrollSlop = 200;
    private bool _isCardMode;
    
    public double ViewportWidth
    {
        get => _viewportWidth;
        set => Set(ref _viewportWidth, value);
    }
    
    public double ViewportHeight
    {
        get => _viewportHeight;
        set => Set(ref _viewportHeight, value);
    }
    
    public double TopInset
    {
        get => _topInset;
        set => Set(ref _topInset, value);
    }
    
    public double OverscrollSlop
    {
        get => _overscrollSlop;
        set => Set(ref _overscrollSlop, value);
    }
    
    public bool IsCardMode
    {
        get => _isCardMode;
        set => Set(ref _isCardMode, value);
    }
    
    public double DetentOffset
        => Math.Max(0, _viewportHeight - _topInset);

    private double SheetHeight
        => Math.Max(0, DetentOffset + _overscrollSlop);

    private double GetWidth(double availableWidth)
    {
        if (_viewportWidth > 0)
            return _viewportWidth;

        return double.IsInfinity(availableWidth) ? 0 : availableWidth;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = GetWidth(availableSize.Width);
        var childHeight = _isCardMode ? _viewportHeight : SheetHeight;

        foreach (var child in Children)
            child.Measure(new Size(width, childHeight));

        return new Size(width, _isCardMode ? _viewportHeight : _viewportHeight + SheetHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = GetWidth(finalSize.Width);

        if (_isCardMode)
        {
            foreach (var child in Children)
                child.Arrange(new Rect(0, 0, width, _viewportHeight));

            return new Size(width, _viewportHeight);
        }

        var sheetHeight = SheetHeight;
        foreach (var child in Children)
            child.Arrange(new Rect(0, _viewportHeight, width, sheetHeight));

        return new Size(width, _viewportHeight + sheetHeight);
    }

    public bool AreHorizontalSnapPointsRegular => false;

    public bool AreVerticalSnapPointsRegular => false;

    // required by IScrollSnapPointsInfo, but this panel never has horizontal snap points
#pragma warning disable CS0067
    public event EventHandler<object> HorizontalSnapPointsChanged;
#pragma warning restore CS0067
    public event EventHandler<object> VerticalSnapPointsChanged;

    public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
    {
        // dismissed, presented. a medium detent slots in between these two.
        if (orientation != Orientation.Vertical || _isCardMode || DetentOffset <= 0)
            return new List<float>();

        return new List<float>() { 0f, (float)DetentOffset };
    }

    public float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset)
    {
        offset = 0;
        throw new NotSupportedException("Sheet snap points are irregular.");
    }

    private void Set(ref double field, double value)
    {
        if (field == value)
            return;

        field = value;
        InvalidateMeasure();
        VerticalSnapPointsChanged?.Invoke(this, null);
    }

    private void Set(ref bool field, bool value)
    {
        if (field == value)
            return;

        field = value;
        InvalidateMeasure();
        VerticalSnapPointsChanged?.Invoke(this, null);
    }
}
