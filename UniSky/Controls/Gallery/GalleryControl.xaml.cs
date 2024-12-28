using System;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Overlay;
using UniSky.Services;
using UniSky.ViewModels.Gallery;

namespace UniSky.Controls.Gallery;

public sealed partial class GalleryControl : StandardOverlayControl
{
    public GalleryControl()
    {
        this.InitializeComponent();
    }

    protected override void OnShowing(OverlayShowingEventArgs args)
    {
        base.OnShowing(args);

        if (args.Parameter is not ShowGalleryArgs gallery)
            throw new InvalidOperationException("Must specify gallery arguments");

        DataContext = ActivatorUtilities.CreateInstance<GalleryViewModel>(ServiceContainer.Scoped, gallery);
    }
}
