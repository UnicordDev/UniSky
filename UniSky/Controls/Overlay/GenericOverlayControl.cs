using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

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
    }

    protected override void OnHidden(RoutedEventArgs args)
    {
        base.OnHidden(args);
    }
}
