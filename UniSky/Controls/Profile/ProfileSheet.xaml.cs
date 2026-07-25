using UniSky.Controls.Overlay;
using UniSky.Controls.Sheet;
using UniSky.Pages;
using UniSky.Services;

namespace UniSky.Controls.Profile;

public sealed partial class ProfileSheet : SheetControl
{
    public ProfileSheet()
    {
        this.InitializeComponent();

        this.Showing += OnShowing;
    }

    private void OnShowing(IOverlayControl sender, OverlayShowingEventArgs args)
    {
        ContentFrame.Navigate(typeof(ProfilePage));
    }
}
