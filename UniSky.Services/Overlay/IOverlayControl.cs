using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniSky.Controls.Overlay;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace UniSky.Services;

public interface IOverlayControl
{
    IOverlayController Controller { get; }
    object OverlayContent { get; set; }
    DataTemplate OverlayContentTemplate { get; set; }
    Size PreferredWindowSize { get; set; }
    object TitleContent { get; set; }
    DataTemplate TitleContentTemplate { get; set; }

    event TypedEventHandler<IOverlayControl, RoutedEventArgs> Hidden;
    event TypedEventHandler<IOverlayControl, OverlayHidingEventArgs> Hiding;
    event TypedEventHandler<IOverlayControl, OverlayShowingEventArgs> Showing;
    event TypedEventHandler<IOverlayControl, RoutedEventArgs> Shown;

    void InvokeHidden();
    Task<bool> InvokeHidingAsync();
    void InvokeShowing(object parameter);
    void InvokeShown();
    void SetOverlayController(IOverlayController controller);
}
