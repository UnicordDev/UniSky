using UniSky.Controls.Overlay;
using UniSky.Navigation;
using UniSky.Services;
using UniSky.Services.Navigation;

namespace UniSky.Controls.Sheet;

/// <summary>
/// A sheet that hosts a navigable frame, shown for any request with
/// <see cref="NavigationIntent.Sheet"/>.
/// </summary>
public sealed partial class NavigationSheet : SheetControl
{
    public NavigationSheet()
    {
        this.InitializeComponent();

        this.Showing += OnShowing;
    }

    private void OnShowing(IOverlayControl sender, OverlayShowingEventArgs args)
    {
        var scope = SheetNavigationScope.Attach(ContentFrame, this);

        if (args.Parameter is NavigationRequest request)
            scope?.Handle(request.WithIntent(NavigationIntent.Default));
    }
}
