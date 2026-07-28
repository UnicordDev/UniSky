using Windows.Devices.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UniSky.Templates;

public partial class FeedTemplates : ResourceDictionary
{
    public FeedTemplates()
    {
        this.InitializeComponent();
    }

    private void OnCarouselPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // touch pans the strip directly, and the chevrons would just be in the way
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            return;

        GoToCarouselState(sender, "ChevronsVisible");
    }

    private void OnCarouselPointerExited(object sender, PointerRoutedEventArgs e)
        => GoToCarouselState(sender, "ChevronsHidden");

    private static void GoToCarouselState(object sender, string state)
    {
        if (sender is Control control)
            VisualStateManager.GoToState(control, state, true);
    }
}
