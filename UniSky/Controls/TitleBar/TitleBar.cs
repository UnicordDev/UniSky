using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace UniSky.Controls;

public sealed class TitleBar : Control
{
    public bool IsBackButtonVisible
    {
        get => (bool)GetValue(IsBackButtonVisibleProperty);
        set => SetValue(IsBackButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsBackButtonVisibleProperty =
        DependencyProperty.Register("IsBackButtonVisible", typeof(bool), typeof(TitleBar), new PropertyMetadata(false));

    public ICommand BackButtonCommand
    {
        get => (ICommand)GetValue(BackButtonCommandProperty);
        set => SetValue(BackButtonCommandProperty, value);
    }

    public static readonly DependencyProperty BackButtonCommandProperty =
        DependencyProperty.Register("BackButtonCommand", typeof(ICommand), typeof(TitleBar), new PropertyMetadata(null));

    private readonly ISafeAreaService safeAreaService
        = ServiceContainer.Scoped.GetRequiredService<ISafeAreaService>();

    public TitleBar()
    {
        this.DefaultStyleKey = typeof(TitleBar);
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var TitleBarDrag = (Border)this.FindDescendantByName("TitleBarDrag");
        Window.Current.SetTitleBar(TitleBarDrag);

        this.safeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.safeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        if (e.SafeArea.HasTitleBar)
        {
            Visibility = Visibility.Visible;
            Height = e.SafeArea.Bounds.Top;
        }
        else
        {
            Visibility = Visibility.Collapsed;
        }

        if (e.SafeArea.IsActive)
        {
            VisualStateManager.GoToState(this, "Active", true);
        }
        else
        {
            VisualStateManager.GoToState(this, "Inactive", true);
        }

        RequestedTheme = e.SafeArea.Theme;
    }
}
