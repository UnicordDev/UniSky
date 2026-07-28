using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Navigation;
using UniSky.Services;
using UniSky.Services.Navigation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UniSky.Controls.Sheet;

public sealed partial class SheetRootControl : UserControl, IOverlayRootControl
{
    private sealed class SheetStackEntry
    {
        public IOverlayController Controller { get; set; }
        public IOverlayControl Control { get; set; }
        public SheetPresenter Presenter { get; set; }
    }
    
    public FrameworkElement BackgroundElement
    {
        get => (FrameworkElement)GetValue(BackgroundElementProperty);
        set => SetValue(BackgroundElementProperty, value);
    }

    public static readonly DependencyProperty BackgroundElementProperty =
        DependencyProperty.Register("BackgroundElement", typeof(FrameworkElement), typeof(SheetRootControl), new PropertyMetadata(null));

    private readonly List<SheetStackEntry> _stack = new List<SheetStackEntry>();
    private readonly ISafeAreaService _safeAreaService;
    private readonly IBackNavigationCoordinator _backCoordinator;
    private readonly IDisposable _backRegistration;

    public SheetRootControl()
    {
        this.InitializeComponent();

        _safeAreaService = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();
        _safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        _backCoordinator = ServiceContainer.Scoped.GetRequiredService<IBackNavigationCoordinator>();
        _backRegistration = _backCoordinator.Register(new SheetBackHandler(this), BackPriority.Sheet);
    }

    private sealed class SheetBackHandler(SheetRootControl owner) : IBackHandler
    {
        public bool CanGoBack
            => owner.Top != null;

        public bool TryGoBack()
        {
            var entry = owner.Top;
            if (entry == null)
                return false;

            if (NavigationScopeHost.GetScopeInstance(entry.Presenter) is { CanGoBack: true } scope)
                return scope.GoBack();
                
            // always consume the back gesture, even if we refuse to close
            _ = entry.Controller.TryHideAsync();
            return true;
        }
    }

    private SheetStackEntry Top
        => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;


    // gives the dispatcher a tick to avoid racing any layout ops
    private Task YieldToDispatcherAsync()
        => Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { }).AsTask();

    private async Task ShowSheetAsync(IOverlayController controller, IOverlayControl control, object parameter)
    {
        await YieldToDispatcherAsync();

        var previousTop = Top;

        var presenter = new SheetPresenter();
        presenter.SetContent((FrameworkElement)control);
        presenter.UpdateSafeArea(_safeAreaService.State.Bounds);
        presenter.DismissRequested += OnPresenterDismissRequested;

        var entry = new SheetStackEntry()
        {
            Controller = controller,
            Control = control,
            Presenter = presenter
        };

        _stack.Add(entry);
        SheetHost.Children.Add(presenter);

        // a sheet is its own navigation scope, and anything in it belongs to the sheet
        NavigationScopeHost.SetKind(presenter, NavigationScopeKind.Sheet);
        NavigationScopeHost.SetPolicy(presenter, NavigationScopePolicy.Contain);
        NavigationScopeHost.SetScope(presenter, $"sheet:{_stack.Count}");
        NavigationScopeHost.EnsureScope(presenter);

        presenter.SetBackdropTarget(previousTop?.Presenter ?? BackgroundElement);
        control.InvokeShowing(parameter);
        
        UpdateInteractivity();

        await presenter.PresentAsync();
        
        if (Top != entry)
            return;

        await FocusSheetAsync(presenter);
        control.InvokeShown();
    }

    private async Task<bool> HideSheetAsync(IOverlayController controller)
    {
        var entry = Top;
        if (entry == null || entry.Controller != controller)
            return false;

        await entry.Presenter.DismissAsync();
        await YieldToDispatcherAsync();

        entry.Presenter.DismissRequested -= OnPresenterDismissRequested;
        entry.Presenter.Teardown();

        NavigationScopeHost.Detach(entry.Presenter);

        _stack.Remove(entry);
        SheetHost.Children.Remove(entry.Presenter);

        UpdateInteractivity();

        entry.Control.InvokeHidden();

        return true;
    }

    private async void OnPresenterDismissRequested(SheetPresenter sender, object args)
    {
        var entry = Top;
        if (entry == null || entry.Presenter != sender)
            return;

        if (!await entry.Controller.TryHideAsync())
            await sender.PresentAsync();
    }

    private void UpdateInteractivity()
    {
        for (var i = 0; i < _stack.Count; i++)
            _stack[i].Presenter.IsHitTestVisible = i == _stack.Count - 1;

        var hasSheets = _stack.Count > 0;
        SheetHost.Visibility = hasSheets ? Visibility.Visible : Visibility.Collapsed;

        if (BackgroundElement != null)
        {
            BackgroundElement.IsHitTestVisible = !hasSheets;
            if (BackgroundElement is Control backgroundControl)
                backgroundControl.IsTabStop = !hasSheets;
        }

        _backCoordinator.Invalidate();
    }

    private static async Task FocusSheetAsync(SheetPresenter presenter)
    {
        var elementToFocus = FocusManager.FindFirstFocusableElement(presenter.SheetContent);
        if (ApiInformation.IsMethodPresent(typeof(FocusManager).FullName, "TryFocusAsync")
            && elementToFocus is DependencyObject dep)
        {
            await FocusManager.TryFocusAsync(dep, FocusState.Programmatic);
        }
        else if (elementToFocus is Control controlToFocus)
        {
            controlToFocus.Focus(FocusState.Programmatic);
        }
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        foreach (var entry in _stack)
            entry.Presenter.UpdateSafeArea(e.SafeArea.Bounds);
    }

    Task IOverlayRootControl.ShowAsync(IOverlayController controller, IOverlayControl control, object param)
        => ShowSheetAsync(controller, control, param);

    Task<bool> IOverlayRootControl.HideAsync(IOverlayController controller)
        => HideSheetAsync(controller);
}
