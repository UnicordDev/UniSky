using FishyFlip.Lexicon.App.Bsky.Embed;
using UniSky.Services.Overlay;
using UniSky.ViewModels.Posts;
using Windows.Foundation;

namespace UniSky.ViewModels.VideoPlayer;

public record ShowVideoPlayerArgs(ViewVideo ViewVideo = null) : IOverlaySizeProvider
{
    public Size? GetDesiredSize()
    {
        if (ViewVideo.AspectRatio != null)
            return new Size(ViewVideo.AspectRatio.Width, ViewVideo.AspectRatio.Height);

        return null;
    }
}

internal class VideoPlayerViewModel : PostEmbedVideoViewModel
{
    public VideoPlayerViewModel(ShowVideoPlayerArgs video) 
        : base(video.ViewVideo)
    {
    }
}
