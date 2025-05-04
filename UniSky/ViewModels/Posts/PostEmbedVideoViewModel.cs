using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FishyFlip.Lexicon.App.Bsky.Embed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Controls;
using UniSky.Controls.VideoPlayer;
using UniSky.Services;
using UniSky.Services.Overlay;
using UniSky.ViewModels.VideoPlayer;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace UniSky.ViewModels.Posts;

public partial class PostEmbedVideoViewModel : PostEmbedViewModel
{
    private readonly ViewVideo video;

    [ObservableProperty]
    private string thumbnailUrl;
    [ObservableProperty]
    private IMediaPlaybackSource source;
    [ObservableProperty]
    private AspectRatioConstraint ratio;

    public PostEmbedVideoViewModel(ViewVideo video)
        : base(video)
    {
        this.video = video;
        this.ThumbnailUrl = video.Thumbnail;
        this.Ratio = video.AspectRatio != null ?
            new AspectRatioConstraint(Math.Max((double)video.AspectRatio.Width / video.AspectRatio.Height, 0.5)) :
            new AspectRatioConstraint(16, 9);

        // todo: lazy
        _ = Task.Run(LoadAsync);
    }

    private async Task LoadAsync()
    {
        var create = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(video.Playlist));
        if (create.Status == AdaptiveMediaSourceCreationStatus.Success)
            Source = MediaSource.CreateFromAdaptiveMediaSource(create.MediaSource);
    }

    [RelayCommand]
    private async Task ShowVideoPlayerAsync(object parameter)
    {
        var settingsService = ServiceContainer.Scoped.GetRequiredService<ITypedSettings>();
        if (parameter is UIElement control && !settingsService.UseMultipleWindows)
        {
            ConnectedAnimationService.GetForCurrentView()
                .PrepareToAnimate("VideoPlayerView", control);
        }

        var genericOverlay = ServiceContainer.Scoped.GetRequiredService<IStandardOverlayService>();
        await genericOverlay.ShowAsync<VideoPlayerOverlay>(new ShowVideoPlayerArgs(video));
    }
}
