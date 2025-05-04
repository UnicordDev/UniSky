using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.DependencyInjection;
using UniSky.Controls.Overlay;
using UniSky.Services;
using UniSky.ViewModels.Gallery;
using UniSky.ViewModels.VideoPlayer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace UniSky.Controls.VideoPlayer;

public sealed partial class VideoPlayerOverlay : StandardOverlayControl
{
    public VideoPlayerOverlay()
    {
        this.InitializeComponent();
    }

    protected override void OnShowing(OverlayShowingEventArgs args)
    {
        base.OnShowing(args);

        if (args.Parameter is not ShowVideoPlayerArgs video)
            throw new InvalidOperationException("Must specify gallery arguments");

        DataContext = ActivatorUtilities.CreateInstance<VideoPlayerViewModel>(ServiceContainer.Scoped, video);

        var animation = ConnectedAnimationService.GetForCurrentView()
            .GetAnimation("VideoPlayerView");

        if (animation != null)
        {
            if (ApiInformation.IsTypePresent(typeof(DirectConnectedAnimationConfiguration).FullName))
                animation.Configuration = new DirectConnectedAnimationConfiguration();
            animation.TryStart(MediaPlayer);
        }
    }

    protected override void OnHidden(RoutedEventArgs args)
    {
        base.OnHidden(args);

        MediaPlayer.MediaPlayer?.Dispose();
    }
}
