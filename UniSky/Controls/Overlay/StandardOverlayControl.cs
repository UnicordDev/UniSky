using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Services;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniSky.Controls.Overlay;

public class StandardOverlayControl : OverlayControl
{
    public StandardOverlayControl()
    {
        this.DefaultStyleKey = typeof(StandardOverlayControl);
    }

    protected override void OnShown(RoutedEventArgs args)
    {
        base.OnShown(args);

        if (Controller.IsStandalone)
        {
            VisualStateManager.GoToState(this, "FullWindow", false);
        }
        else
        {
            VisualStateManager.GoToState(this, "Standard", false);
        }

        var TitleBarDragArea = this.FindDescendantByName("TitleBarDragArea");
        Controller.SafeAreaService.SetTitleBar(TitleBarDragArea);
        Controller.SafeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;
    }

    protected override void OnHidden(RoutedEventArgs args)
    {
        base.OnHidden(args);

        Controller.SafeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        var TitleBar = (Grid)this.FindDescendantByName("TitleBarGrid");
        var TitlePresenter = (ContentPresenter)this.FindDescendantByName("SheetTitlePresenter");
        if (TitleBar != null)
        {
            if (TitlePresenter != null)
            {
                if (e.SafeArea.HasTitleBar)
                {
                    TitlePresenter.Visibility = Visibility.Visible;
                }
                else
                {
                    TitlePresenter.Visibility = Visibility.Collapsed;
                }
            }

            TitleBar.Height = e.SafeArea.Bounds.Top;
        }

        Margin = e.SafeArea.Bounds with { Top = 0 };
    }
}
