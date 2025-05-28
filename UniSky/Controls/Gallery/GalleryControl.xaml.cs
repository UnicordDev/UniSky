using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using UniSky.Controls.Overlay;
using UniSky.Services;
using UniSky.ViewModels.Gallery;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

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

    protected override void OnShown(RoutedEventArgs args)
    {
        base.OnShown(args);
    }

    private void MainImage_Loaded(object sender, RoutedEventArgs e)
    {
        if (Controller.IsStandalone)
            return;

        var vm = (GalleryViewModel)DataContext;
        var selected = vm.Images[vm.SelectedIndex];
        var source = (ImageEx)sender;

        if (source.Tag != selected)
            return;

        var container = FlippyView.ContainerFromIndex(vm.SelectedIndex);

        var animation = ConnectedAnimationService.GetForCurrentView()
            .GetAnimation("GalleryView");

        if (animation != null)
        {
            if (ApiInformation.IsTypePresent(typeof(DirectConnectedAnimationConfiguration).FullName))
                animation.Configuration = new DirectConnectedAnimationConfiguration();

            animation.TryStart(source);
        }
    }
}
