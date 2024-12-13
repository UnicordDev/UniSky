using Windows.UI.Xaml;

namespace UniSky.Controls.Overlay;

public class OverlayShowingEventArgs : RoutedEventArgs
{
    public object Parameter { get; }

    public OverlayShowingEventArgs(object parameter)
    {
        Parameter = parameter;
    }
}
