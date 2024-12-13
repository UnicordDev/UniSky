using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Controls.Overlay;
using UniSky.Services;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace UniSky.Controls.Sheet;

public class SheetControl : OverlayControl
{
    public object PrimaryButtonContent
    {
        get => (object)GetValue(PrimaryButtonContentProperty);
        set => SetValue(PrimaryButtonContentProperty, value);
    }

    public static readonly DependencyProperty PrimaryButtonContentProperty =
        DependencyProperty.Register("PrimaryButtonContent", typeof(object), typeof(SheetControl), new PropertyMetadata(null));

    public DataTemplate PrimaryButtonContentTemplate
    {
        get => (DataTemplate)GetValue(PrimaryButtonContentTemplateProperty);
        set => SetValue(PrimaryButtonContentTemplateProperty, value);
    }

    public static readonly DependencyProperty PrimaryButtonContentTemplateProperty =
        DependencyProperty.Register("PrimaryButtonContentTemplate", typeof(DataTemplate), typeof(SheetControl), new PropertyMetadata(null));

    public Visibility PrimaryButtonVisibility
    {
        get => (Visibility)GetValue(PrimaryButtonVisibilityProperty);
        set => SetValue(PrimaryButtonVisibilityProperty, value);
    }

    public static readonly DependencyProperty PrimaryButtonVisibilityProperty =
        DependencyProperty.Register("PrimaryButtonVisibility", typeof(Visibility), typeof(SheetControl), new PropertyMetadata(Visibility.Visible));

    public ICommand PrimaryButtonCommand
    {
        get => (ICommand)GetValue(PrimaryButtonCommandProperty);
        set => SetValue(PrimaryButtonCommandProperty, value);
    }

    public static readonly DependencyProperty PrimaryButtonCommandProperty =
        DependencyProperty.Register("PrimaryButtonCommand", typeof(ICommand), typeof(SheetControl), new PropertyMetadata(null));

    public bool IsPrimaryButtonEnabled
    {
        get => (bool)GetValue(IsPrimaryButtonEnabledProperty);
        set => SetValue(IsPrimaryButtonEnabledProperty, value);
    }

    public static readonly DependencyProperty IsPrimaryButtonEnabledProperty =
        DependencyProperty.Register("IsPrimaryButtonEnabled", typeof(bool), typeof(SheetControl), new PropertyMetadata(true));

    public object SecondaryButtonContent
    {
        get => (object)GetValue(SecondaryButtonContentProperty);
        set => SetValue(SecondaryButtonContentProperty, value);
    }

    public static readonly DependencyProperty SecondaryButtonContentProperty =
        DependencyProperty.Register("SecondaryButtonContent", typeof(object), typeof(SheetControl), new PropertyMetadata(null));

    public DataTemplate SecondaryButtonContentTemplate
    {
        get => (DataTemplate)GetValue(SecondaryButtonContentTemplateProperty);
        set => SetValue(SecondaryButtonContentTemplateProperty, value);
    }

    public static readonly DependencyProperty SecondaryButtonContentTemplateProperty =
        DependencyProperty.Register("SecondaryButtonContentTemplate", typeof(DataTemplate), typeof(SheetControl), new PropertyMetadata(null));

    public Visibility SecondaryButtonVisibility
    {
        get => (Visibility)GetValue(SecondaryButtonVisibilityProperty);
        set => SetValue(SecondaryButtonVisibilityProperty, value);
    }

    public static readonly DependencyProperty SecondaryButtonVisibilityProperty =
        DependencyProperty.Register("SecondaryButtonVisibility", typeof(Visibility), typeof(SheetControl), new PropertyMetadata(Visibility.Visible));

    public ICommand SecondaryButtonCommand
    {
        get => (ICommand)GetValue(SecondaryButtonCommandProperty);
        set => SetValue(SecondaryButtonCommandProperty, value);
    }

    public static readonly DependencyProperty SecondaryButtonCommandProperty =
        DependencyProperty.Register("SecondaryButtonCommand", typeof(ICommand), typeof(SheetControl), new PropertyMetadata(null));

    public bool IsSecondaryButtonEnabled
    {
        get => (bool)GetValue(IsSecondaryButtonEnabledProperty);
        set => SetValue(IsSecondaryButtonEnabledProperty, value);
    }

    public static readonly DependencyProperty IsSecondaryButtonEnabledProperty =
        DependencyProperty.Register("IsSecondaryButtonEnabled", typeof(bool), typeof(SheetControl), new PropertyMetadata(true));

    public SheetControl()
    {
        this.DefaultStyleKey = typeof(SheetControl);

        // default hide
        this.SecondaryButtonCommand = new AsyncRelayCommand(() =>
        {
            return Controller?.TryHideSheetAsync();
        });
    }

    protected override void OnHidden(RoutedEventArgs args)
    {
        if (Controller.IsFullWindow)
        {
            Controller.SafeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;
        }

        base.OnHidden(args);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (Controller != null && Controller.IsFullWindow)
        {
            VisualStateManager.GoToState(this, "FullWindow", false);
            var titleBarDragArea = this.FindDescendantByName("TitleBarDragArea");
            Controller.SafeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;
            Controller.SafeAreaService.SetTitleBar(titleBarDragArea);

            var inputPane = InputPane.GetForCurrentView();
            inputPane.Showing += OnInputPaneShowing;
            inputPane.Hiding += OnInputPaneHiding;

            this.SizeChanged += OnSizeChanged;
        }
        else
        {
            VisualStateManager.GoToState(this, "Standard", false);
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var rightButton = this.FindDescendantByName("PrimaryTitleBarButton");
        this.OnBottomInsetsChanged(0, rightButton.ActualWidth + 16);
    }

    private void OnInputPaneShowing(InputPane sender, InputPaneVisibilityEventArgs args)
    {
        var ButtonsGrid = (Grid)this.FindDescendantByName("ButtonsGrid");
        ButtonsGrid.Margin = new Thickness(0, 0, 0, args.OccludedRect.Height);
        args.EnsuredFocusedElementInView = true;
    }

    private void OnInputPaneHiding(InputPane sender, InputPaneVisibilityEventArgs args)
    {
        var ButtonsGrid = (Grid)this.FindDescendantByName("ButtonsGrid");
        ButtonsGrid.Margin = new Thickness(0, 0, 0, args.OccludedRect.Height);
        args.EnsuredFocusedElementInView = true;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        var titleBarGrid = (Grid)this.FindDescendantByName("TitleBarGrid");

        if (e.SafeArea.HasTitleBar)
        {
            titleBarGrid.Height = e.SafeArea.Bounds.Top;
            titleBarGrid.Padding = new Thickness();
        }
        else
        {
            titleBarGrid.Height = 42;
            titleBarGrid.Padding = new Thickness(0, e.SafeArea.Bounds.Top, 0, 4);
        }

        Margin = new Thickness(e.SafeArea.Bounds.Left, 0, e.SafeArea.Bounds.Right, e.SafeArea.Bounds.Bottom);
    }

    protected virtual void OnBottomInsetsChanged(double leftInset, double rightInset) { }
}
