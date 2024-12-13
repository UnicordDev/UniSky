using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Overlay;
using UniSky.Services;
using UniSky.ViewModels.Gallery;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Controls.Gallery;

public sealed partial class GalleryControl : GenericOverlayControl
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
