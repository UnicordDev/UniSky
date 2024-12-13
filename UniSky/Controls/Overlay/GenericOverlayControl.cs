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

public class GenericOverlayControl : OverlayControl
{
    public GenericOverlayControl()
    {
        this.DefaultStyleKey = typeof(GenericOverlayControl);
    }

    protected override void OnShown(RoutedEventArgs args)
    {
        base.OnShown(args);

        var TitleBarDragArea = this.FindDescendantByName("TitleBarDragArea");
        Controller.SafeAreaService.SetTitleBar(TitleBarDragArea);
        Controller.SafeAreaService.SafeAreaUpdated += OnSafeAreaUpdated;

        if (Controller.IsStandalone)
        {
            VisualStateManager.GoToState(this, "FullWindow", false);
        }
        else
        {
            VisualStateManager.GoToState(this, "Standard", false);
        }
    }

    protected override void OnHidden(RoutedEventArgs args)
    {
        base.OnHidden(args);

        Controller.SafeAreaService.SafeAreaUpdated -= OnSafeAreaUpdated;
    }

    private void OnSafeAreaUpdated(object sender, SafeAreaUpdatedEventArgs e)
    {
        var TitleBar = (Grid)this.FindDescendantByName("TitleBarGrid");

        TitleBar.Height = e.SafeArea.Bounds.Top;
        Margin = e.SafeArea.Bounds with { Top = 0 };
    }
}
